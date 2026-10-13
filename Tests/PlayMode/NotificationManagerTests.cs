using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

namespace Geuneda.NotificationService.Tests
{
	/// <summary>
	/// <see cref="GameNotificationsMonoBehaviour"/> 의 게이트, 권한 요청, 전환 처리를 가짜 플랫폼으로 검증합니다.
	/// </summary>
	[TestFixture]
	public class NotificationManagerTests
	{
		private FakeNotificationsPlatform _platform;

		[SetUp]
		public void SetUp()
		{
			NotificationTestUtils.DeletePrefs();
			_platform = new FakeNotificationsPlatform();
		}

		[TearDown]
		public void TearDown()
		{
			NotificationTestUtils.DestroyAllManagers();
			NotificationTestUtils.DeletePrefs();
		}

		[Test]
		public void SetNotificationsEnabled_False_CancelsDismissesAndClearsPendingAndSavedList()
		{
			var manager = CreateManager();
			manager.ScheduleNotification(CreateFutureNotification());
			PlayerPrefs.SetString(GameNotificationsMonoBehaviour.NotificationsPrefsKey, "saved");

			manager.SetNotificationsEnabled(false);

			Assert.AreEqual(1, _platform.CancelAllCount);
			Assert.AreEqual(1, _platform.DismissAllCount);
			Assert.AreEqual(0, manager.PendingNotifications.Count);
			Assert.IsFalse(PlayerPrefs.HasKey(GameNotificationsMonoBehaviour.NotificationsPrefsKey));
			Assert.IsNull(manager.ScheduleNotification(CreateFutureNotification()));
			Assert.AreEqual(1, _platform.ScheduledNotifications.Count);
		}

		[Test]
		public void Initialize_Disabled_CancelsOsScheduleAndDoesNotRestoreSavedList()
		{
			SaveFutureNotification(5);
			PlayerPrefs.SetInt(GameNotificationsMonoBehaviour.EnabledPrefsKey, 0);

			var manager = CreateManager();

			Assert.IsFalse(manager.NotificationsEnabled);
			Assert.AreEqual(1, _platform.CancelAllCount);
			Assert.AreEqual(0, manager.PendingNotifications.Count);
		}

		[Test]
		public void Initialize_Enabled_RestoresSavedList()
		{
			SaveFutureNotification(5);

			var manager = CreateManager();

			Assert.AreEqual(0, _platform.CancelAllCount);
			Assert.AreEqual(1, manager.PendingNotifications.Count);
			Assert.AreEqual(5, manager.PendingNotifications[0].Notification.Id);
		}

		[Test]
		public void SetNotificationsEnabled_TrueAgain_HandlerCanReschedule()
		{
			var manager = CreateManager();
			var changes = new List<bool>();
			manager.OnNotificationsEnabledChanged = enabled =>
			{
				changes.Add(enabled);

				if (enabled)
				{
					manager.ScheduleNotification(CreateFutureNotification());
				}
			};

			manager.SetNotificationsEnabled(false);
			manager.SetNotificationsEnabled(true);

			CollectionAssert.AreEqual(new[] { false, true }, changes);
			Assert.AreEqual(1, manager.PendingNotifications.Count);
			Assert.AreEqual(1, _platform.ScheduledNotifications.Count);
		}

		[Test]
		public void RequestPermission_WhilePending_ReportsPendingAndMergesRequests()
		{
			var manager = CreateManager();
			var results = new List<NotificationPermission>();

			manager.RequestPermission(results.Add);
			manager.RequestPermission(results.Add);

			Assert.AreEqual(1, _platform.PermissionRequestCount);
			Assert.AreEqual(NotificationPermission.Pending, manager.Permission);
			CollectionAssert.IsEmpty(results);

			_platform.LastPermissionRequest.Complete(NotificationPermission.Granted);
			_platform.CurrentPermission = NotificationPermission.Granted;
			manager.SendMessage("Update");

			CollectionAssert.AreEqual(new[] { NotificationPermission.Granted, NotificationPermission.Granted }, results);
			Assert.AreEqual(NotificationPermission.Granted, manager.Permission);
		}

		[Test]
		public void RequestPermission_AlreadyDecided_CompletesImmediately()
		{
			_platform.ImmediatePermissionResult = NotificationPermission.Denied;
			var manager = CreateManager();
			var results = new List<NotificationPermission>();

			manager.RequestPermission(results.Add);

			CollectionAssert.AreEqual(new[] { NotificationPermission.Denied }, results);
		}

		[Test]
		public void RequestPermission_CallbackThrows_OtherCallbacksStillNotified()
		{
			var manager = CreateManager();
			var results = new List<NotificationPermission>();

			manager.RequestPermission(_ => throw new InvalidOperationException("boom"));
			manager.RequestPermission(results.Add);
			_platform.LastPermissionRequest.Complete(NotificationPermission.Denied);

			LogAssert.Expect(LogType.Exception, new Regex("boom"));
			manager.SendMessage("Update");

			CollectionAssert.AreEqual(new[] { NotificationPermission.Denied }, results);
		}

		[Test]
		public void Destroy_WhilePermissionPending_DropsCallbacksAndUnsubscribesPlatform()
		{
			var manager = CreateManager();
			var called = false;

			manager.RequestPermission(_ => called = true);
			Assert.IsTrue(_platform.HasReceivedSubscribers);

			Object.DestroyImmediate(manager.gameObject);
			_platform.LastPermissionRequest.Complete(NotificationPermission.Granted);

			Assert.IsFalse(called);
			Assert.IsFalse(_platform.HasReceivedSubscribers);
		}

		[Test]
		public void OnApplicationPause_FalseWhileForeground_KeepsQueuedNotifications()
		{
			var manager = CreateManager(OperatingMode.Queue);
			manager.ScheduleNotification(CreateFutureNotification());

			manager.SendMessage("OnApplicationPause", false);

			Assert.AreEqual(1, manager.PendingNotifications.Count);
			Assert.AreEqual(0, _platform.ScheduledNotifications.Count);
		}

		[Test]
		public void OnApplicationPause_True_SchedulesQueuedNotificationsAndSaves()
		{
			var manager = CreateManager(OperatingMode.Queue);
			manager.ScheduleNotification(CreateFutureNotification());

			manager.SendMessage("OnApplicationPause", true);

			Assert.AreEqual(1, _platform.ScheduledNotifications.Count);
			Assert.IsTrue(PlayerPrefs.HasKey(GameNotificationsMonoBehaviour.NotificationsPrefsKey));
		}

		[Test]
		public void ScheduleNotification_AutoId_SkipsIdAlreadyPending()
		{
			var manager = CreateManager();
			var explicitNotification = CreateFutureNotification();
			explicitNotification.Id = GameNotificationsMonoBehaviour.FirstAutoNotificationId;
			manager.ScheduleNotification(explicitNotification);

			var pending = manager.ScheduleNotification(CreateFutureNotification());

			Assert.AreEqual(GameNotificationsMonoBehaviour.FirstAutoNotificationId + 1, pending.Notification.Id);
		}

#if UNITY_IOS
		[Test]
		public void IOSNotification_NewInstance_HasNoIdAndReceivesAutoId()
		{
			_platform.NotificationFactory = () => new iOSGameNotification();
			var manager = CreateManager();
			var notification = manager.CreateNotification();

			Assert.IsNull(notification.Id);

			notification.DeliveryTime = DateTime.Now.AddHours(1);
			var pending = manager.ScheduleNotification(notification);

			Assert.AreEqual(GameNotificationsMonoBehaviour.FirstAutoNotificationId, pending.Notification.Id);
			LogAssert.NoUnexpectedReceived();
		}

		[Test]
		public void IOSNotification_ExplicitId_IsKept()
		{
			var notification = new iOSGameNotification { Id = 42 };

			Assert.AreEqual(42, notification.Id);
		}
#endif

		private GameNotificationsMonoBehaviour CreateManager(OperatingMode mode = OperatingMode.NoQueue)
		{
			var manager = new GameObject("NotificationManagerTest").AddComponent<GameNotificationsMonoBehaviour>();
			manager.Mode = mode;
			manager.InitializeWithPlatform(_platform);

			return manager;
		}

		private static FakeGameNotification CreateFutureNotification()
		{
			return new FakeGameNotification { DeliveryTime = DateTime.Now.AddHours(1) };
		}

		private static void SaveFutureNotification(int id)
		{
			var saved = new PendingNotification(new FakeGameNotification
			{
				Id = id,
				DeliveryTime = DateTime.Now.AddHours(1),
				Scheduled = true
			}).AsSerializableNotification();

			PlayerPrefs.SetString(GameNotificationsMonoBehaviour.NotificationsPrefsKey,
				SerializableNotificationConverter.ToJson(new[] { saved }));
		}
	}
}

// MobileNotificationService 의 에디터 전용 분기(예약하지 않음, 권한은 항상 Granted)를 검증하므로 에디터에서만 실행한다.
// 관리자 로직은 플레이어에서도 도는 NotificationManagerTests 가 가짜 플랫폼으로 검증한다
#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace Geuneda.NotificationService.Tests
{
	/// <summary>
	/// 앱 안 알림 설정, OS 권한(에디터), 자동 ID 부여를 검증합니다.
	/// 에디터에서는 플랫폼 예약을 하지 않으므로 서비스가 직접 지키는 동작만 확인합니다.
	/// </summary>
	[TestFixture]
	public class NotificationGateTests
	{
		[SetUp]
		public void SetUp()
		{
			NotificationTestUtils.DeletePrefs();
		}

		[TearDown]
		public void TearDown()
		{
			NotificationTestUtils.DestroyAllManagers();
			NotificationTestUtils.DeletePrefs();
		}

		[Test]
		public void IsEnabled_Default_IsTrueAndSchedules()
		{
			var service = new MobileNotificationService();

			Assert.IsTrue(service.IsEnabled);
			Assert.IsNotNull(service.ScheduleNotification(service.CreateNotification()));
		}

		[Test]
		public void IsEnabled_SetFalse_RaisesEventOnceAndRefusesSchedule()
		{
			var service = new MobileNotificationService();
			var changes = new List<bool>();
			service.OnEnabledChanged += changes.Add;

			service.IsEnabled = false;
			service.IsEnabled = false;

			CollectionAssert.AreEqual(new[] { false }, changes);
			Assert.IsFalse(service.IsEnabled);
			Assert.IsNull(service.ScheduleNotification(service.CreateNotification()));
		}

		[Test]
		public void IsEnabled_SetTrueAgain_RaisesEventAndSchedules()
		{
			var service = new MobileNotificationService();
			var changes = new List<bool>();
			service.OnEnabledChanged += changes.Add;

			service.IsEnabled = false;
			service.IsEnabled = true;

			CollectionAssert.AreEqual(new[] { false, true }, changes);
			Assert.IsNotNull(service.ScheduleNotification(service.CreateNotification()));
		}

		[Test]
		public void IsEnabled_Disabled_PersistsToNextInstance()
		{
			var first = new MobileNotificationService();
			first.IsEnabled = false;
			NotificationTestUtils.DestroyAllManagers();

			var second = new MobileNotificationService();

			Assert.IsFalse(second.IsEnabled);
			Assert.IsNull(second.ScheduleNotification(second.CreateNotification()));
		}

		[Test]
		public void ScheduleNotification_WithoutId_AssignsIncreasingIdsAcrossInstances()
		{
			var first = new MobileNotificationService();
			var id1 = first.ScheduleNotification(first.CreateNotification()).Notification.Id;
			var id2 = first.ScheduleNotification(first.CreateNotification()).Notification.Id;
			NotificationTestUtils.DestroyAllManagers();

			var second = new MobileNotificationService();
			var id3 = second.ScheduleNotification(second.CreateNotification()).Notification.Id;

			var firstAutoId = GameNotificationsMonoBehaviour.FirstAutoNotificationId;

			Assert.AreEqual(firstAutoId, id1);
			Assert.AreEqual(firstAutoId + 1, id2);
			Assert.AreEqual(firstAutoId + 2, id3);
		}

		[Test]
		public void ScheduleNotification_WithExplicitId_KeepsId()
		{
			var service = new MobileNotificationService();
			var notification = service.CreateNotification();
			notification.Id = 1000;

			var pending = service.ScheduleNotification(notification);

			Assert.AreEqual(1000, pending.Notification.Id);
		}

		[Test]
		public void Permission_InEditor_IsGrantedAndRequestCompletesImmediately()
		{
			var service = new MobileNotificationService();
			var results = new List<NotificationPermission>();

			service.RequestPermission(results.Add);

			Assert.AreEqual(NotificationPermission.Granted, service.Permission);
			CollectionAssert.AreEqual(new[] { NotificationPermission.Granted }, results);
		}
	}
}
#endif

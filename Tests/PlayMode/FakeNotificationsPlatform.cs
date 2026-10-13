using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace

namespace Geuneda.NotificationService.Tests
{
	/// <summary>
	/// 관리자 로직을 실제 OS 없이 검증하기 위한 플랫폼. 호출을 기록하고 권한 요청 결과를 테스트가 정합니다.
	/// </summary>
	internal sealed class FakeNotificationsPlatform : IGameNotificationsPlatform
	{
		public event Action<IGameNotification> NotificationReceived;

		public readonly List<IGameNotification> ScheduledNotifications = new List<IGameNotification>();
		public int CancelAllCount;
		public int DismissAllCount;
		public int PermissionRequestCount;
		public NotificationPermission CurrentPermission = NotificationPermission.NotDetermined;
		// null 이 아니면 권한 요청이 요청 창 없이 이 결과로 바로 끝난다
		public NotificationPermission? ImmediatePermissionResult;
		public FakePermissionRequest LastPermissionRequest;
		public Func<IGameNotification> NotificationFactory = () => new FakeGameNotification();

		public bool HasReceivedSubscribers => NotificationReceived != null;

		public IGameNotification CreateNotification()
		{
			return NotificationFactory();
		}

		public void ScheduleNotification(IGameNotification gameNotification)
		{
			ScheduledNotifications.Add(gameNotification);

			if (gameNotification is FakeGameNotification fake)
			{
				fake.Scheduled = true;
			}
		}

		public void CancelNotification(int notificationId)
		{
		}

		public void DismissNotification(int notificationId)
		{
		}

		public void CancelAllScheduledNotifications()
		{
			CancelAllCount++;
		}

		public void DismissAllDisplayedNotifications()
		{
			DismissAllCount++;
		}

		public void OnForeground()
		{
		}

		public void OnBackground()
		{
		}

		public NotificationPermission GetPermission()
		{
			return CurrentPermission;
		}

		public IPermissionRequestOperation RequestPermission()
		{
			PermissionRequestCount++;
			LastPermissionRequest = new FakePermissionRequest();

			if (ImmediatePermissionResult.HasValue)
			{
				LastPermissionRequest.Complete(ImmediatePermissionResult.Value);
			}

			return LastPermissionRequest;
		}
	}

	internal sealed class FakePermissionRequest : IPermissionRequestOperation
	{
		public bool IsDone { get; private set; }

		public NotificationPermission Result { get; private set; } = NotificationPermission.Pending;

		public void Complete(NotificationPermission result)
		{
			IsDone = true;
			Result = result;
		}
	}

	internal sealed class FakeGameNotification : IGameNotification
	{
		public int? Id { get; set; }
		public string Title { get; set; }
		public string Body { get; set; }
		public string Subtitle { get; set; }
		public string Channel { get; set; }
		public int? BadgeNumber { get; set; }
		public bool ShouldAutoCancel { get; set; }
		public DateTime? DeliveryTime { get; set; }
		public bool Scheduled { get; set; }
		public string SmallIcon { get; set; }
		public string LargeIcon { get; set; }
	}
}

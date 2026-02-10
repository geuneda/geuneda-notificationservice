#if UNITY_IOS
using System;
using Unity.Notifications.iOS;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// <see cref="IGameNotificationsPlatform"/>의 iOS 구현.
    /// </summary>
    internal class iOSNotificationsPlatform : IGameNotificationsPlatform<iOSGameNotification>, IDisposable
    {
        /// <inheritdoc />
        public event Action<IGameNotification> NotificationReceived;

        /// <summary>
        /// <see cref="iOSNotificationsPlatform"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public iOSNotificationsPlatform()
        {
            iOSNotificationCenter.OnNotificationReceived += OnLocalNotificationReceived;
        }

        /// <inheritdoc />
        public void ScheduleNotification(IGameNotification gameNotification)
        {
            if (gameNotification == null)
            {
                throw new ArgumentNullException(nameof(gameNotification));
            }

            if (!(gameNotification is iOSGameNotification notification))
            {
                throw new InvalidOperationException(
                    "Notification provided to ScheduleNotification isn't an iOSGameNotification.");
            }

            ScheduleNotification(notification);
        }

        /// <inheritdoc />
        public void ScheduleNotification(iOSGameNotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            iOSNotificationCenter.ScheduleNotification(notification.InternalNotification);
            notification.OnScheduled();
        }

        /// <inheritdoc />
        /// <summary>
        /// 새 <see cref="T:NotificationSamples.Android.AndroidNotification" />을 생성합니다.
        /// </summary>
        IGameNotification IGameNotificationsPlatform.CreateNotification()
        {
            return CreateNotification();
        }

        /// <inheritdoc />
        /// <summary>
        /// 새 <see cref="T:NotificationSamples.Android.AndroidNotification" />을 생성합니다.
        /// </summary>
        public iOSGameNotification CreateNotification()
        {
            return new iOSGameNotification();
        }

        /// <inheritdoc />
        public void CancelNotification(int notificationId)
        {
            iOSNotificationCenter.RemoveScheduledNotification(notificationId.ToString());
        }

        /// <inheritdoc />
        public void DismissNotification(int notificationId)
        {
            iOSNotificationCenter.RemoveDeliveredNotification(notificationId.ToString());
        }

        /// <inheritdoc />
        public void CancelAllScheduledNotifications()
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();
        }

        /// <inheritdoc />
        public void DismissAllDisplayedNotifications()
        {
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }

        /// <summary>
        /// 배지 카운트를 초기화합니다.
        /// </summary>
        public void OnForeground()
        {
            iOSNotificationCenter.ApplicationBadge = 0;
        }

        /// <summary>
        /// iOS에서는 아무 작업도 수행하지 않습니다.
        /// </summary>
        public void OnBackground() {}

        /// <summary>
        /// 델리게이트 등록을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            iOSNotificationCenter.OnNotificationReceived -= OnLocalNotificationReceived;
        }

        // 로컬 알림 수신을 위한 이벤트 핸들러.
        private void OnLocalNotificationReceived(iOSNotification notification)
        {
            // 전달된 알림으로 새 AndroidGameNotification을 생성하되,
            // 이벤트가 등록된 경우에만 수행
            NotificationReceived?.Invoke(new iOSGameNotification(notification));
        }
    }
}
#endif

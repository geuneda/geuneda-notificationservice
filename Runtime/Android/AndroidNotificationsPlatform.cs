#if UNITY_ANDROID
using System;
using System.Linq;
using Unity.Notifications.Android;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// <see cref="IGameNotificationsPlatform"/>의 Android 구현.
    /// </summary>
    internal class AndroidNotificationsPlatform : IGameNotificationsPlatform<AndroidGameNotification>,
        IDisposable
    {
        /// <inheritdoc />
        public event Action<IGameNotification> NotificationReceived;

        /// <summary>
        /// 알림의 기본 채널 ID를 가져오거나 설정합니다.
        /// </summary>
        /// <value>새 알림의 기본 채널 ID, 또는 null.</value>
        public string DefaultChannelId { get; set; }

        /// <summary>
        /// <see cref="AndroidNotificationsPlatform"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public AndroidNotificationsPlatform()
        {
            AndroidNotificationCenter.OnNotificationReceived += OnLocalNotificationReceived;
        }

        /// <summary>
        /// 주어진 <seealso cref="AndroidNotificationChannel"/>을 Android에 등록합니다
        /// </summary>
        public void RegisterChannel(GameNotificationChannel notificationChannel)
        {
            long[] vibrationPattern = null;
            if (notificationChannel.VibrationPattern != null)
            {
                vibrationPattern = notificationChannel.VibrationPattern.Select(v => (long)v).ToArray();
            }
            
            var channel = new AndroidNotificationChannel(notificationChannel.Id, notificationChannel.Name,
                notificationChannel.Description, (Importance)notificationChannel.Style)
            {
                CanBypassDnd = notificationChannel.HighPriority,
                CanShowBadge = notificationChannel.ShowsBadge,
                EnableLights = notificationChannel.ShowLights,
                EnableVibration = notificationChannel.Vibrates,
                LockScreenVisibility = (LockScreenVisibility)notificationChannel.Privacy,
                VibrationPattern = vibrationPattern
            };

            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }

        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="gameNotification"/>의 <see cref="AndroidGameNotification.Id"/> 필드를 설정합니다.
        /// </remarks>
        public void ScheduleNotification(AndroidGameNotification gameNotification)
        {
            if (gameNotification == null)
            {
                throw new ArgumentNullException(nameof(gameNotification));
            }

            if (gameNotification.Id.HasValue)
            {
                AndroidNotificationCenter.SendNotificationWithExplicitID(gameNotification.InternalNotification,
                    gameNotification.DeliveredChannel,
                    gameNotification.Id.Value);
            }
            else
            {
                int notificationId = AndroidNotificationCenter.SendNotification(gameNotification.InternalNotification,
                    gameNotification.DeliveredChannel);
                gameNotification.Id = notificationId;
            }

            gameNotification.OnScheduled();
        }

        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="gameNotification"/>의 <see cref="AndroidGameNotification.Id"/> 필드를 설정합니다.
        /// </remarks>
        public void ScheduleNotification(IGameNotification gameNotification)
        {
            if (gameNotification == null)
            {
                throw new ArgumentNullException(nameof(gameNotification));
            }

            if (!(gameNotification is AndroidGameNotification androidNotification))
            {
                throw new InvalidOperationException(
                    "Notification provided to ScheduleNotification isn't an AndroidGameNotification.");
            }

            ScheduleNotification(androidNotification);
        }

        /// <inheritdoc />
        /// <summary>
        /// 새 <see cref="AndroidGameNotification" />을 생성합니다.
        /// </summary>
        public AndroidGameNotification CreateNotification()
        {
            var notification = new AndroidGameNotification()
            {
                DeliveredChannel = DefaultChannelId
            };

            return notification;
        }

        /// <inheritdoc />
        /// <summary>
        /// 새 <see cref="AndroidGameNotification" />을 생성합니다.
        /// </summary>
        IGameNotification IGameNotificationsPlatform.CreateNotification()
        {
            return CreateNotification();
        }

        /// <inheritdoc />
        public void CancelNotification(int notificationId)
        {
            AndroidNotificationCenter.CancelScheduledNotification(notificationId);
        }

        /// <inheritdoc />
        /// <summary>
        /// Android에서는 현재 구현되지 않음
        /// </summary>
        public void DismissNotification(int notificationId)
        {
            AndroidNotificationCenter.CancelDisplayedNotification(notificationId);
        }

        /// <inheritdoc />
        public void CancelAllScheduledNotifications()
        {
            AndroidNotificationCenter.CancelAllScheduledNotifications();
        }

        /// <inheritdoc />
        public void DismissAllDisplayedNotifications()
        {
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
        }

        /// <summary>
        /// Android에서는 아무 작업도 수행하지 않습니다.
        /// </summary>
        public void OnForeground() {}

        /// <summary>
        /// Android에서는 아무 작업도 수행하지 않습니다.
        /// </summary>
        public void OnBackground() {}

        /// <summary>
        /// 델리게이트 등록을 해제합니다.
        /// </summary>
        public void Dispose()
        {
            AndroidNotificationCenter.OnNotificationReceived -= OnLocalNotificationReceived;
        }

        // 로컬 알림 수신을 위한 이벤트 핸들러.
        private void OnLocalNotificationReceived(AndroidNotificationIntentData data)
        {
            // 전달된 알림으로 새 AndroidGameNotification을 생성하되,
            // 이벤트가 등록된 경우에만 수행
            NotificationReceived?.Invoke(new AndroidGameNotification(data.Notification, data.Id, data.Channel));
        }
    }
}
#endif

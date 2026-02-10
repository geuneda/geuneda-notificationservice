#if UNITY_ANDROID
using System;
using Unity.Notifications.Android;
using UnityEngine.Assertions;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// <see cref="IGameNotification"/>의 Android 전용 구현.
    /// </summary>
    public class AndroidGameNotification : IGameNotification
    {
        private AndroidNotification internalNotification;

        /// <summary>
        /// 모바일 알림 시스템에서 사용하는 내부 알림 객체를 가져옵니다.
        /// </summary>
        public AndroidNotification InternalNotification => internalNotification;

        /// <inheritdoc />
        /// <summary>
        /// Android에서 ID가 명시적으로 설정되지 않은 경우 예약 후 자동으로 생성됩니다.
        /// </summary>
        public int? Id { get; set; }

        /// <inheritdoc />
        public string Title { get => InternalNotification.Title; set => internalNotification.Title = value; }

        /// <inheritdoc />
        public string Body { get => InternalNotification.Text; set => internalNotification.Text = value; }

        /// <summary>
        /// Android에서는 아무 작업도 수행하지 않습니다.
        /// </summary>
        public string Subtitle { get => null; set {} }

        /// <inheritdoc />
        /// <remarks>
        /// Android에서는 알림의 채널을 나타내며 필수입니다. <see cref="AndroidNotificationsPlatform.DefaultChannelId"/>가 설정된 경우
        /// <see cref="AndroidNotificationsPlatform"/>에 의해 자동으로 구성됩니다
        /// </remarks>
        /// <value><see cref="DeliveredChannel"/>의 값.</value>
        public string Channel { get => DeliveredChannel; set => DeliveredChannel = value; }

        /// <inheritdoc />
        public int? BadgeNumber
        {
            get => internalNotification.Number != -1 ? internalNotification.Number : (int?)null;
            set => internalNotification.Number = value ?? -1;
        }

        /// <inheritdoc />
        public bool ShouldAutoCancel
        {
            get => InternalNotification.ShouldAutoCancel;
            set => internalNotification.ShouldAutoCancel = value;
        }

        /// <inheritdoc />
        public DateTime? DeliveryTime
        {
            get => InternalNotification.FireTime;
            set => internalNotification.FireTime = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 이 알림의 채널을 가져오거나 설정합니다.
        /// </summary>
        public string DeliveredChannel { get; set; }

        /// <inheritdoc />
        public bool Scheduled { get; private set; }

        /// <inheritdoc />
        public string SmallIcon { get => InternalNotification.SmallIcon; set => internalNotification.SmallIcon = value; }

        /// <inheritdoc />
        public string LargeIcon { get => InternalNotification.LargeIcon; set => internalNotification.LargeIcon = value; }

        /// <summary>
        /// <see cref="AndroidGameNotification"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public AndroidGameNotification()
        {
            internalNotification = new AndroidNotification();
        }

        /// <summary>
        /// 전달된 알림으로부터 <see cref="AndroidGameNotification"/>의 새 인스턴스를 생성합니다
        /// </summary>
        /// <param name="deliveredNotification">전달된 알림.</param>
        /// <param name="deliveredId">전달된 알림의 ID.</param>
        /// <param name="deliveredChannel">알림이 전달된 채널.</param>
        internal AndroidGameNotification(AndroidNotification deliveredNotification, int deliveredId,
                                         string deliveredChannel)
        {
            internalNotification = deliveredNotification;
            Id = deliveredId;
            DeliveredChannel = deliveredChannel;
        }

        /// <summary>
        /// 예약 완료 플래그를 설정합니다.
        /// </summary>
        internal void OnScheduled()
        {
            Assert.IsFalse(Scheduled);
            Scheduled = true;
        }
    }
}
#endif

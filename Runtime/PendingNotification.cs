using System;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// <see cref="GameNotificationsMonoBehaviour.ScheduleNotification"/>으로 예약된 알림을 나타냅니다.
    /// </summary>
    public class PendingNotification
    {
        /// <summary>
        /// 앱이 다시 포그라운드로 전환될 때 표시되지 않은 이 이벤트를 재예약할지 여부.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="GameNotificationsMonoBehaviour"/>의 <see cref="GameNotificationsMonoBehaviour.Mode"/>
        /// 플래그가 <see cref="OperatingMode.RescheduleAfterClearing"/>으로 설정된 경우에만 유효합니다.
        /// </para>
        /// <para>
        /// iOS 위치 알림과 같이 시간 기반이 아닌 전달 예약 방법을 사용하는 알림에는
        /// 작동하지 않습니다.
        /// </para>
        /// </remarks>
        public bool Reschedule;

        /// <summary>
        /// 예약된 알림.
        /// </summary>
        public readonly IGameNotification Notification;

        /// <summary>
        /// <see cref="IGameNotification"/>으로부터 <see cref="PendingNotification"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="notification">생성할 원본 알림.</param>
        public PendingNotification(IGameNotification notification)
        {
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
        }
    }
}

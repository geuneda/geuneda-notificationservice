using System;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// 특정 게임 플랫폼의 알림을 처리하는 타입
    /// </summary>
    internal interface IGameNotificationsPlatform
    {
        /// <summary>
        /// 알림이 수신될 때 발생합니다.
        /// </summary>
        event Action<IGameNotification> NotificationReceived;

        /// <summary>
        /// 이 플랫폼에 대한 <see cref="IGameNotification"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>새로운 플랫폼에 적합한 알림 객체.</returns>
        IGameNotification CreateNotification();

        /// <summary>
        /// 알림 전달을 예약합니다.
        /// </summary>
        /// <param name="gameNotification">전달할 알림.</param>
        /// <exception cref="ArgumentNullException"><paramref name="gameNotification"/>이 null인 경우.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="gameNotification"/>이 올바른 타입이 아닌 경우.</exception>
        void ScheduleNotification(IGameNotification gameNotification);

        /// <summary>
        /// 예약된 알림을 취소합니다.
        /// </summary>
        /// <param name="notificationId">이전에 예약된 알림의 ID.</param>
        void CancelNotification(int notificationId);

        /// <summary>
        /// 표시된 알림을 닫습니다.
        /// </summary>
        /// <param name="notificationId">사용자에게 표시 중인 이전에 예약된 알림의 ID.</param>
        void DismissNotification(int notificationId);

        /// <summary>
        /// 모든 예약된 알림을 취소합니다.
        /// </summary>
        void CancelAllScheduledNotifications();

        /// <summary>
        /// 모든 표시된 알림을 닫습니다.
        /// </summary>
        void DismissAllDisplayedNotifications();

        /// <summary>
        /// 애플리케이션을 포그라운드로 전환할 때 필요한 초기화 또는 처리를 수행합니다.
        /// </summary>
        void OnForeground();

        /// <summary>
        /// 애플리케이션을 백그라운드로 전환하거나 종료할 때 필요한 처리를 수행합니다.
        /// </summary>
        void OnBackground();
    }

    /// <summary>
    /// 특정 게임 플랫폼의 알림을 처리하는 타입.
    /// </summary>
    /// <remarks>구체적인 알림 타입을 가짐</remarks>
    /// <typeparam name="TNotificationType">이 플랫폼이 반환하는 알림 타입.</typeparam>
    internal interface IGameNotificationsPlatform<TNotificationType> : IGameNotificationsPlatform
        where TNotificationType : IGameNotification
    {
        /// <summary>
        /// <typeparamref name="TNotificationType"/>의 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>새로운 플랫폼에 적합한 알림 객체.</returns>
        new TNotificationType CreateNotification();

        /// <summary>
        /// 알림 전달을 예약합니다.
        /// </summary>
        /// <param name="notification">전달할 알림.</param>
        /// <exception cref="ArgumentNullException"><paramref name="notification"/>이 null인 경우.</exception>
        void ScheduleNotification(TNotificationType notification);
    }
}

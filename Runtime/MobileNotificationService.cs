using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
	/// <summary>
	/// 현재 플랫폼에서 알림을 예약하고 처리할 수 있는 서비스
	/// </summary>
	public interface INotificationService
	{
		/// <summary>
		/// 앱이 포그라운드에 있는 동안 예약된 로컬 알림이 전달될 때 발생하는 이벤트.
		/// </summary>
		event Action<PendingNotification> OnLocalNotificationDeliveredEvent;

		/// <summary>
		/// 표시되어야 할 시점에 애플리케이션이 포그라운드에 있어서 큐에 있는 로컬 알림이 취소될 때 발생하는 이벤트.
		/// </summary>
		/// <seealso cref="OperatingMode.Queue"/>
		event Action<PendingNotification> OnLocalNotificationExpiredEvent;

		/// <summary>
		/// 예약되었거나 큐에 있는 알림 컬렉션을 가져옵니다.
		/// </summary>
		IReadOnlyList<PendingNotification> PendingNotifications { get; }
		
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
		PendingNotification ScheduleNotification(IGameNotification gameNotification);

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
	}
	
	/// <inheritdoc />
	public class MobileNotificationService : INotificationService
	{
		private readonly GameNotificationsMonoBehaviour _monoBehaviour;

		/// <inheritdoc />
		public event Action<PendingNotification> OnLocalNotificationDeliveredEvent;
		/// <inheritdoc />
		public event Action<PendingNotification> OnLocalNotificationExpiredEvent;

		/// <inheritdoc />
		public IReadOnlyList<PendingNotification> PendingNotifications => _monoBehaviour.PendingNotifications;
		
		public MobileNotificationService(params GameNotificationChannel[] channels)
		{
			_monoBehaviour = new GameObject("NotificationService").AddComponent<GameNotificationsMonoBehaviour>();
			_monoBehaviour.OnLocalNotificationDelivered = OnLocalNotificationDeliveredEvent;
			_monoBehaviour.OnLocalNotificationExpired = OnLocalNotificationExpiredEvent;

			_monoBehaviour.Initialize(channels);
			UnityEngine.Object.DontDestroyOnLoad(_monoBehaviour);
		}

		/// <inheritdoc />
		public IGameNotification CreateNotification()
		{
#if UNITY_EDITOR
			return new EditorGameNotification();
#else
			return _monoBehaviour.CreateNotification();
#endif
		}

		/// <inheritdoc />
		public PendingNotification ScheduleNotification(IGameNotification gameNotification)
		{
#if UNITY_EDITOR
			if (!gameNotification.Id.HasValue)
            {
                // ID가 없는 항목에 대해 ID를 생성 (나중에 식별할 수 있도록)
                gameNotification.Id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
            }
			return new PendingNotification(gameNotification);
#else
			return _monoBehaviour.ScheduleNotification(gameNotification);
#endif
		}

		/// <inheritdoc />
		public void CancelNotification(int notificationId)
		{
			_monoBehaviour.CancelNotification(notificationId);
		}

		/// <inheritdoc />
		public void DismissNotification(int notificationId)
		{
			_monoBehaviour.DismissNotification(notificationId);
		}

		/// <inheritdoc />
		public void CancelAllScheduledNotifications()
		{
			_monoBehaviour.CancelAllNotifications();
		}

		/// <inheritdoc />
		public void DismissAllDisplayedNotifications()
		{
			_monoBehaviour.DismissAllNotifications();
		}
	}
}
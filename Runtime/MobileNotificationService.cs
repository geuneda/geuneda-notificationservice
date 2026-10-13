using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
	/// <summary>
	/// 현재 플랫폼에서 알림을 예약하고 처리할 수 있는 서비스
	/// </summary>
	/// <remarks>
	/// 알림은 두 게이트를 모두 통과해야 사용자에게 보입니다.
	/// 1. 앱 안 알림 설정(<see cref="IsEnabled"/>): 서비스가 지킵니다. 꺼져 있으면 예약하지 않습니다.
	/// 2. OS 알림 권한(<see cref="Permission"/>): OS 가 지킵니다. <see cref="RequestPermission"/> 으로 요청합니다.
	/// 설정 화면에 보여 줄 "알림 켜짐" 값은 <c>IsEnabled &amp;&amp; Permission == NotificationPermission.Granted</c> 입니다.
	/// </remarks>
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
		/// 앱 안 알림 설정(<see cref="IsEnabled"/>)이 바뀔 때 발생하는 이벤트. 인자는 바뀐 값입니다.
		/// 켜졌을 때 필요한 알림을 다시 예약하는 데 씁니다.
		/// </summary>
		event Action<bool> OnEnabledChanged;

		/// <summary>
		/// 예약되었거나 큐에 있는 알림 컬렉션을 가져옵니다.
		/// </summary>
		IReadOnlyList<PendingNotification> PendingNotifications { get; }

		/// <summary>
		/// 앱 안 알림 설정을 가져오거나 바꿉니다(기본값 true). 값은 PlayerPrefs 에 저장되어 다음 실행에도 유지됩니다.
		/// </summary>
		/// <remarks>
		/// false 로 바꾸면 예약된 알림을 모두 취소하고 표시된 알림을 닫으며, 이후 <see cref="ScheduleNotification"/> 은
		/// 예약하지 않고 null 을 반환합니다. true 로 바꿔도 지운 알림을 되살리지 않으므로 <see cref="OnEnabledChanged"/> 에서
		/// 필요한 알림을 다시 예약하세요.
		/// </remarks>
		bool IsEnabled { get; set; }

		/// <summary>
		/// 현재 OS 알림 권한 상태를 가져옵니다. 사용자가 기기 설정에서 바꿀 수 있으므로 필요할 때마다 다시 읽습니다.
		/// </summary>
		/// <remarks>
		/// 권한이 없어도 예약 호출은 막지 않습니다. Android 는 나중에 권한을 받으면 이미 예약한 알림이 표시되지만,
		/// iOS 는 권한이 없는 동안 OS 가 예약을 버리므로 권한을 받은 뒤 다시 예약해야 합니다.
		/// 에디터에서는 항상 <see cref="NotificationPermission.Granted"/> 입니다.
		/// </remarks>
		NotificationPermission Permission { get; }

		/// <summary>
		/// OS 알림 권한을 요청하고 결과를 <paramref name="onCompleted"/> 로 알립니다.
		/// Android 13 이상(targetSdk 33 이상)에서는 POST_NOTIFICATIONS 권한, iOS 에서는 알림·배지·소리 권한을 요청합니다.
		/// </summary>
		/// <remarks>
		/// 이미 결정되어 요청 창이 뜨지 않으면 바로 알립니다. 진행 중인 요청이 있으면 그 결과를 함께 받습니다.
		/// 에디터에서는 바로 <see cref="NotificationPermission.Granted"/> 로 알립니다.
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="onCompleted"/>가 null 인 경우.</exception>
		void RequestPermission(Action<NotificationPermission> onCompleted);
		
		/// <summary>
		/// 이 플랫폼에 대한 <see cref="IGameNotification"/>의 새 인스턴스를 생성합니다.
		/// </summary>
		/// <returns>새로운 플랫폼에 적합한 알림 객체.</returns>
		IGameNotification CreateNotification();

		/// <summary>
		/// 알림 전달을 예약합니다. ID 가 없으면 단조 증가 ID 를 부여합니다(다음 실행에도 이어짐).
		/// </summary>
		/// <param name="gameNotification">전달할 알림.</param>
		/// <returns>예약된 알림. 앱 안 알림 설정(<see cref="IsEnabled"/>)이 꺼져 있으면 예약하지 않고 null.</returns>
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
		public event Action<bool> OnEnabledChanged;

		/// <inheritdoc />
		public IReadOnlyList<PendingNotification> PendingNotifications => _monoBehaviour.PendingNotifications;

		/// <inheritdoc />
		public bool IsEnabled
		{
			get => _monoBehaviour.NotificationsEnabled;
			set => _monoBehaviour.SetNotificationsEnabled(value);
		}

		/// <inheritdoc />
		public NotificationPermission Permission
		{
			get
			{
#if UNITY_EDITOR
				return NotificationPermission.Granted;
#else
				return _monoBehaviour.Permission;
#endif
			}
		}

		/// <summary>
		/// 배지 번호를 직접 지정하지 않은 알림에 배지 번호를 자동으로 매길지 여부입니다(기본값 true).
		/// 내부 <see cref="GameNotificationsMonoBehaviour.AutoBadging"/>을 읽고 씁니다.
		/// </summary>
		public bool AutoBadging
		{
			get => _monoBehaviour.AutoBadging;
			set => _monoBehaviour.AutoBadging = value;
		}
		
		public MobileNotificationService(params GameNotificationChannel[] channels) : this(OperatingMode.NoQueue, channels)
		{
		}

		/// <summary>
		/// 주어진 <paramref name="mode"/>로 동작하는 서비스를 생성합니다.
		/// </summary>
		/// <param name="mode">내부 <see cref="GameNotificationsMonoBehaviour.Mode"/>에 설정할 동작 모드.</param>
		/// <param name="channels">Android용으로 등록할 선택적 채널 컬렉션</param>
		public MobileNotificationService(OperatingMode mode, params GameNotificationChannel[] channels)
		{
			_monoBehaviour = new GameObject("NotificationService").AddComponent<GameNotificationsMonoBehaviour>();
			// Initialize가 포그라운드 처리에서 Mode를 사용하므로 그 전에 설정
			_monoBehaviour.Mode = mode;
			// 생성 이후에 구독한 핸들러도 호출되도록 호출 시점의 이벤트를 전달
			_monoBehaviour.OnLocalNotificationDelivered = notification => OnLocalNotificationDeliveredEvent?.Invoke(notification);
			_monoBehaviour.OnLocalNotificationExpired = notification => OnLocalNotificationExpiredEvent?.Invoke(notification);
			_monoBehaviour.OnNotificationsEnabledChanged = enabled => OnEnabledChanged?.Invoke(enabled);

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
			if (gameNotification == null)
			{
				throw new ArgumentNullException(nameof(gameNotification));
			}

#if UNITY_EDITOR
			if (!_monoBehaviour.NotificationsEnabled)
			{
				return null;
			}

			if (!gameNotification.Id.HasValue)
			{
				// ID가 없는 항목에 대해 단조 증가 ID를 부여 (나중에 식별할 수 있도록)
				gameNotification.Id = _monoBehaviour.AllocateNotificationId();
			}
			return new PendingNotification(gameNotification);
#else
			return _monoBehaviour.ScheduleNotification(gameNotification);
#endif
		}

		/// <inheritdoc />
		public void RequestPermission(Action<NotificationPermission> onCompleted)
		{
#if UNITY_EDITOR
			if (onCompleted == null)
			{
				throw new ArgumentNullException(nameof(onCompleted));
			}

			onCompleted(NotificationPermission.Granted);
#else
			_monoBehaviour.RequestPermission(onCompleted);
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
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// 알림 관리자의 동작 모드
    /// </summary>
    [Flags]
    public enum OperatingMode
    {
        /// <summary>
        /// 큐잉을 전혀 수행하지 않습니다. 모든 알림이 운영 체제에 즉시 예약됩니다.
        /// </summary>
        NoQueue = 0x00,

        /// <summary>
        /// <para>
        /// 이 관리자로 예약된 메시지를 큐에 넣습니다.
        /// 애플리케이션이 백그라운드로 전환될 때까지 운영 체제에 메시지가 전송되지 않습니다.
        /// </para>
        /// <para>
        /// 배지 번호가 설정되지 않은 경우 자동으로 증가시킵니다. 이는 대기 중인 알림에
        /// 배지 번호가 한 번도 설정되지 않은 경우에만 발생합니다.
        /// </para>
        /// </summary>
        Queue = 0x01,

        /// <summary>
        /// 애플리케이션이 포그라운드로 전환될 때 모든 대기 중인 알림을 지웁니다.
        /// </summary>
        ClearOnForegrounding = 0x02,

        /// <summary>
        /// 이벤트를 지운 후 <see cref="PendingNotification.Reschedule"/>로 표시된 미래의 이벤트를 큐에 다시 넣습니다.
        /// </summary>
        /// <remarks>
        /// <see cref="ClearOnForegrounding"/>도 설정된 경우에만 유효합니다.
        /// </remarks>
        RescheduleAfterClearing = 0x04,

        /// <summary>
        /// <see cref="Queue"/>와 <see cref="ClearOnForegrounding"/>의 동작을 결합합니다.
        /// </summary>
        QueueAndClear = Queue | ClearOnForegrounding,

        /// <summary>
        /// <para>
        /// <see cref="Queue"/>, <see cref="ClearOnForegrounding"/>,
        /// <see cref="RescheduleAfterClearing"/>의 동작을 결합합니다.
        /// </para>
        /// <para>
        /// 애플리케이션이 포그라운드에 있는 동안 메시지가 표시되지 않도록 보장합니다.
        /// </para>
        /// </summary>
        QueueClearAndReschedule = Queue | ClearOnForegrounding | RescheduleAfterClearing,
    }
        
    /// <summary>
    /// 여러 플랫폼의 알림 시스템에 대한 래퍼 역할을 하는 글로벌 알림 관리자입니다.
    /// </summary>
    public sealed class GameNotificationsMonoBehaviour : MonoBehaviour
    {
        
        
        // 백그라운드로 전환 시 알림이 큐에 추가되기 위해 미래 시점이어야 하는 최소 시간
        private static readonly TimeSpan _minimumNotificationTime = new TimeSpan(0, 0, 2);

        // 예약 목록을 저장하는 PlayerPrefs 키
        internal const string NotificationsPrefsKey = "notifications";
        // 앱 안 알림 설정을 저장하는 PlayerPrefs 키(1 켬, 0 끔)
        internal const string EnabledPrefsKey = "geuneda.notificationservice.enabled";
        // 마지막으로 자동 부여한 알림 ID 를 저장하는 PlayerPrefs 키
        internal const string LastIdPrefsKey = "geuneda.notificationservice.lastId";
        // 자동 ID 범위의 시작. 게임이 직접 지정하는 ID(이 값 미만)와 겹치지 않게 한다
        internal const int FirstAutoNotificationId = 1_000_000;

        /// <summary>
        /// 알림 관리자의 동작 모드
        /// </summary>
        public OperatingMode Mode = OperatingMode.NoQueue;

        /// <summary>
        /// 알림 관리자가 배지 번호를 자동으로 증가시키도록 설정합니다.
        /// 이 기능을 사용하려면 번호를 수동으로 설정하지 않고 알림을 예약하세요.
        /// </summary>
        public bool AutoBadging = true;

        /// <summary>
        /// 앱이 포그라운드에 있는 동안 예약된 로컬 알림이 전달될 때 발생하는 이벤트입니다.
        /// </summary>
        public Action<PendingNotification> OnLocalNotificationDelivered;

        /// <summary>
        /// 표시되어야 할 시점에 애플리케이션이 포그라운드에 있어서 큐에 있는 로컬 알림이 취소될 때 발생하는 이벤트입니다.
        /// </summary>
        /// <seealso cref="OperatingMode.Queue"/>
        public Action<PendingNotification> OnLocalNotificationExpired;

        /// <summary>
        /// 앱 안 알림 설정(<see cref="NotificationsEnabled"/>)이 바뀔 때 발생하는 이벤트입니다. 인자는 바뀐 값입니다.
        /// </summary>
        public Action<bool> OnNotificationsEnabledChanged;

        private IGameNotificationsPlatform _platform;
        private bool _inForeground = true;
        private bool _notificationsEnabled = true;
        private int _lastNotificationId;
        private IPermissionRequestOperation _permissionRequest;
        private readonly List<Action<NotificationPermission>> _permissionCallbacks = new List<Action<NotificationPermission>>();

        /// <summary>
        /// 예약되었거나 큐에 있는 알림 컬렉션을 가져옵니다.
        /// </summary>
        public List<PendingNotification> PendingNotifications { get; private set; } = new List<PendingNotification>();

        /// <summary>
        /// 이 관리자가 초기화되었는지 여부를 가져옵니다.
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// 앱 안 알림 설정을 가져옵니다. 바꾸려면 <see cref="SetNotificationsEnabled"/> 를 씁니다.
        /// </summary>
        public bool NotificationsEnabled => _notificationsEnabled;

        /// <summary>
        /// 현재 OS 알림 권한 상태를 가져옵니다. 지원하지 않는 플랫폼에서는 <see cref="NotificationPermission.Granted"/> 입니다.
        /// </summary>
        /// <remarks>
        /// <see cref="RequestPermission"/> 으로 시작한 요청에 사용자가 아직 응답하지 않았으면 <see cref="NotificationPermission.Pending"/> 입니다.
        /// </remarks>
        public NotificationPermission Permission
        {
            get
            {
                if (_platform == null)
                {
                    return NotificationPermission.Granted;
                }

                return _permissionRequest != null ? NotificationPermission.Pending : _platform.GetPermission();
            }
        }

        /// <summary>
        /// 필요한 경우 플랫폼 객체를 정리합니다
        /// </summary>
        private void OnDestroy()
        {
            if (_platform == null)
            {
                return;
            }

            _platform.NotificationReceived -= OnNotificationReceived;
            if (_platform is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _inForeground = false;
            _permissionRequest = null;
            _permissionCallbacks.Clear();
        }

        /// <summary>
        /// 큐 모드일 때 대기 목록에서 만료된 알림을 확인합니다.
        /// </summary>
        private void Update()
        {
            UpdatePermissionRequest();

            if ((Mode & OperatingMode.Queue) != OperatingMode.Queue)
            {
                return;
            }

            // 각 대기 중인 알림의 만료 여부를 확인한 후 제거
            for (int i = PendingNotifications.Count - 1; i >= 0; --i)
            {
                // 만료 콜백에서 취소 API 로 목록이 줄어들 수 있으므로 범위를 매번 다시 확인한다(남은 항목은 다음 프레임에 다시 검사)
                if (i >= PendingNotifications.Count)
                {
                    continue;
                }

                PendingNotification queuedNotification = PendingNotifications[i];
                DateTime? time = queuedNotification.Notification.DeliveryTime;
                if (time != null && time < DateTime.Now)
                {
                    PendingNotifications.RemoveAt(i);
                    OnLocalNotificationExpired?.Invoke(queuedNotification);
                }
            }
        }

        /// <summary>
        /// 애플리케이션 포그라운드/백그라운드 전환에 응답합니다.
        /// </summary>
        /// <remarks>
        /// 포커스(<c>OnApplicationFocus</c>)가 아니라 일시 정지를 기준으로 합니다. 결제창이나 광고처럼 앱 위에 창이 떠서
        /// 포커스만 잃은 경우에는 백그라운드로 보지 않습니다. 시작 직후처럼 상태가 바뀌지 않은 호출은 무시합니다.
        /// </remarks>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (_platform == null || !Initialized)
            {
                return;
            }

            var inForeground = !pauseStatus;

            if (_inForeground == inForeground)
            {
                return;
            }

            _inForeground = inForeground;

            if (inForeground)
            {
                OnForegrounding();

                return;
            }

            _platform.OnBackground();

            // 백그라운드 전환. 미래 날짜의 알림을 큐에 추가
            if ((Mode & OperatingMode.Queue) == OperatingMode.Queue)
            {
                // 과거 이벤트 필터링
                for (var i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    PendingNotification pendingNotification = PendingNotifications[i];
                    // 이미 예약된 알림은 무시
                    if (pendingNotification.Notification.Scheduled)
                    {
                        continue;
                    }

                    // 예약되지 않은 알림이 과거이거나 (또는 임계값 이내가 아닌 경우)
                    // 즉시 제거
                    if (pendingNotification.Notification.DeliveryTime != null &&
                        pendingNotification.Notification.DeliveryTime - DateTime.Now < _minimumNotificationTime)
                    {
                        PendingNotifications.RemoveAt(i);
                    }
                }

                // 배지 번호가 설정된 알림이 없는 경우 전달 시간순으로 알림을 정렬
                bool noBadgeNumbersSet =
                    PendingNotifications.All(notification => notification.Notification.BadgeNumber == null);

                if (noBadgeNumbersSet && AutoBadging)
                {
                    PendingNotifications.Sort((a, b) =>
                    {
                        if (!a.Notification.DeliveryTime.HasValue)
                        {
                            return 1;
                        }

                        if (!b.Notification.DeliveryTime.HasValue)
                        {
                            return -1;
                        }

                        return a.Notification.DeliveryTime.Value.CompareTo(b.Notification.DeliveryTime.Value);
                    });

                    // 배지 번호를 순차적으로 설정
                    var badgeNum = 1;
                    foreach (var pendingNotification in PendingNotifications)
                    {
                        if (pendingNotification.Notification.DeliveryTime.HasValue &&
                            !pendingNotification.Notification.Scheduled)
                        {
                            pendingNotification.Notification.BadgeNumber = badgeNum++;
                        }
                    }
                }

                for (int i = PendingNotifications.Count - 1; i >= 0; i--)
                {
                    var pendingNotification = PendingNotifications[i];
                    // 이미 예약된 알림은 무시
                    if (pendingNotification.Notification.Scheduled)
                    {
                        continue;
                    }

                    // 지금 예약
                    _platform.ScheduleNotification(pendingNotification.Notification);
                }

                // 배지 번호를 다시 지움 (저장용)
                if (noBadgeNumbersSet && AutoBadging)
                {
                    foreach (var pendingNotification in PendingNotifications)
                    {
                        if (pendingNotification.Notification.DeliveryTime.HasValue)
                        {
                            pendingNotification.Notification.BadgeNumber = null;
                        }
                    }
                }
            }

            // 저장할 알림 계산
            var notificationsToSave = new List<SerializableNotification>(PendingNotifications.Count);
            foreach (var pendingNotification in PendingNotifications)
            {
                // 클리어 모드인 경우 재예약 모드가 아니면 아무것도 추가하지 않음
                // 그 외에는 모두 추가
                if ((Mode & OperatingMode.ClearOnForegrounding) == OperatingMode.ClearOnForegrounding)
                {
                    if ((Mode & OperatingMode.RescheduleAfterClearing) != OperatingMode.RescheduleAfterClearing)
                    {
                        continue;
                    }

                    // 재예약 모드에서는 예약되었고, 재예약으로 표시되었으며,
                    // 시간이 설정된 알림을 추가
                    if (pendingNotification.Reschedule &&
                        pendingNotification.Notification.Scheduled &&
                        pendingNotification.Notification.DeliveryTime.HasValue)
                    {
                        notificationsToSave.Add(pendingNotification.AsSerializableNotification());
                    }
                }
                else
                {
                    // 비클리어 모드에서는 예약된 모든 알림을 추가
                    if (pendingNotification.Notification.Scheduled)
                    {
                        notificationsToSave.Add(pendingNotification.AsSerializableNotification());
                    }
                }
            }

            // 디스크에 저장. 백그라운드에서 OS 가 프로세스를 바로 종료할 수 있으므로 즉시 기록한다
            PlayerPrefs.SetString(NotificationsPrefsKey, SerializableNotificationConverter.ToJson(notificationsToSave));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 알림 관리자를 초기화합니다.
        /// </summary>
        /// <param name="channels">Android용으로 등록할 선택적 채널 컬렉션</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 이미 호출된 경우.</exception>
        public void Initialize(params GameNotificationChannel[] channels)
        {
            if (Initialized)
            {
                throw new InvalidOperationException("NotificationsManager already initialized.");
            }

            IGameNotificationsPlatform platform = null;

#if UNITY_ANDROID
            var androidPlatform = new AndroidNotificationsPlatform();

            try
            {
                // 알림 채널 등록
                var doneDefault = false;
                foreach (var notificationChannel in channels)
                {
                    if (!doneDefault)
                    {
                        doneDefault = true;
                        androidPlatform.DefaultChannelId = notificationChannel.Id;
                    }

                    androidPlatform.RegisterChannel(notificationChannel);
                }
            }
            catch
            {
                // 생성자에서 구독한 정적 수신 이벤트가 남지 않도록 실패한 플랫폼을 정리한다(다시 Initialize 할 수 있다)
                androidPlatform.Dispose();
                throw;
            }

            platform = androidPlatform;
#elif UNITY_IOS
            platform = new iOSNotificationsPlatform();
#endif

            InitializeWithPlatform(platform);
        }

        /// <summary>
        /// 주어진 플랫폼 구현으로 초기화합니다. 테스트에서 플랫폼을 바꿔 끼울 때 씁니다.
        /// </summary>
        /// <param name="platform">사용할 플랫폼 구현. null 이면 지원하지 않는 플랫폼처럼 동작합니다.</param>
        /// <exception cref="InvalidOperationException">이미 초기화된 경우.</exception>
        internal void InitializeWithPlatform(IGameNotificationsPlatform platform)
        {
            if (Initialized)
            {
                throw new InvalidOperationException("NotificationsManager already initialized.");
            }

            Initialized = true;
            _notificationsEnabled = PlayerPrefs.GetInt(EnabledPrefsKey, 1) != 0;
            _lastNotificationId = PlayerPrefs.GetInt(LastIdPrefsKey, 0);
            _platform = platform;

            if (_platform == null)
            {
                return;
            }

            _platform.NotificationReceived += OnNotificationReceived;

            OnForegrounding();
        }

        /// <summary>
        /// 현재 플랫폼에 대한 새 알림 객체를 생성합니다.
        /// </summary>
        /// <returns>예약할 준비가 된 새 알림, 또는 유효한 플랫폼이 없는 경우 null.</returns>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 호출되지 않은 경우.</exception>
        public IGameNotification CreateNotification()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            return _platform?.CreateNotification();
        }

        /// <summary>
        /// 알림 전달을 예약합니다.
        /// </summary>
        /// <param name="notification">전달할 알림.</param>
        /// <returns>예약된 알림. <paramref name="notification"/>이 null 이거나 앱 안 알림 설정이 꺼져 있으면 null.</returns>
        public PendingNotification ScheduleNotification(IGameNotification notification)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (notification == null || !_notificationsEnabled)
            {
                return null;
            }

            // ID가 없으면 단조 증가 ID를 부여 (나중에 식별하고 플랫폼 간에 같은 방식으로 취소할 수 있도록)
            if (!notification.Id.HasValue)
            {
                notification.Id = AllocateNotificationId();
            }

            // 큐 모드이면 즉시 예약하지 않음.
            // 또한 시간 기반이 아닌 전달은 즉시 예약 (iOS용)
            if ((Mode & OperatingMode.Queue) != OperatingMode.Queue || notification.DeliveryTime == null)
            {
                _platform?.ScheduleNotification(notification);
            }

            // 대기 중인 알림 등록
            var result = new PendingNotification(notification);
            PendingNotifications.Add(result);

            return result;
        }

        /// <summary>
        /// 예약된 알림을 취소합니다.
        /// </summary>
        /// <param name="notificationId">취소할 알림의 ID.</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 호출되지 않은 경우.</exception>
        public void CancelNotification(int notificationId)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (_platform == null)
            {
                return;
            }

            _platform.CancelNotification(notificationId);

            // 예약 목록에서 취소된 알림을 제거
            var index = PendingNotifications.FindIndex(scheduledNotification =>
                scheduledNotification.Notification.Id == notificationId);

            if (index >= 0)
            {
                PendingNotifications.RemoveAt(index);
            }
        }

        /// <summary>
        /// 모든 예약된 알림을 취소합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 호출되지 않은 경우.</exception>
        public void CancelAllNotifications()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (_platform == null)
            {
                return;
            }

            _platform.CancelAllScheduledNotifications();

            PendingNotifications.Clear();
        }

        /// <summary>
        /// 표시된 알림을 닫습니다.
        /// </summary>
        /// <param name="notificationId">닫을 알림의 ID.</param>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 호출되지 않은 경우.</exception>
        public void DismissNotification(int notificationId)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            _platform?.DismissNotification(notificationId);
        }

        /// <summary>
        /// 모든 표시된 알림을 닫습니다.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 호출되지 않은 경우.</exception>
        public void DismissAllNotifications()
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            _platform?.DismissAllDisplayedNotifications();
        }

        /// <summary>
        /// 앱 안 알림 설정을 바꿉니다. 값은 PlayerPrefs 에 저장되어 다음 실행에도 유지됩니다.
        /// </summary>
        /// <remarks>
        /// 끄면 예약된 알림을 모두 취소하고 표시된 알림을 닫으며, 저장해 둔 예약 목록도 지웁니다.
        /// 켜도 지운 알림을 되살리지 않으므로 <see cref="OnNotificationsEnabledChanged"/> 에서 필요한 알림을 다시 예약합니다.
        /// 값이 그대로이면 아무것도 하지 않습니다.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 호출되지 않은 경우.</exception>
        public void SetNotificationsEnabled(bool enabled)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (_notificationsEnabled == enabled)
            {
                return;
            }

            _notificationsEnabled = enabled;
            PlayerPrefs.SetInt(EnabledPrefsKey, enabled ? 1 : 0);

            if (!enabled)
            {
                _platform?.CancelAllScheduledNotifications();
                _platform?.DismissAllDisplayedNotifications();
                PendingNotifications.Clear();
                PlayerPrefs.DeleteKey(NotificationsPrefsKey);
            }

            PlayerPrefs.Save();
            OnNotificationsEnabledChanged?.Invoke(enabled);
        }

        /// <summary>
        /// OS 알림 권한을 요청하고 결과를 <paramref name="onCompleted"/> 로 알립니다.
        /// </summary>
        /// <remarks>
        /// 이미 결정되어 요청 창 없이 끝나면 바로 알립니다. 진행 중인 요청이 있으면 새로 요청하지 않고 그 결과를 함께 받습니다.
        /// 지원하지 않는 플랫폼에서는 바로 <see cref="NotificationPermission.Granted"/> 로 알립니다.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="onCompleted"/>가 null 인 경우.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Initialize"/>가 호출되지 않은 경우.</exception>
        public void RequestPermission(Action<NotificationPermission> onCompleted)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (onCompleted == null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            if (_platform == null)
            {
                onCompleted(NotificationPermission.Granted);

                return;
            }

            _permissionCallbacks.Add(onCompleted);

            if (_permissionRequest != null)
            {
                return;
            }

            _permissionRequest = _platform.RequestPermission();
            UpdatePermissionRequest();
        }

        /// <summary>
        /// 자동으로 부여할 다음 알림 ID 를 반환합니다. <see cref="FirstAutoNotificationId"/> 부터 1씩 늘고(끝에 닿으면 처음으로)
        /// PlayerPrefs 에 저장되어 다음 실행에도 이어지며, 대기 목록에 같은 ID 가 있으면 건너뜁니다.
        /// </summary>
        internal int AllocateNotificationId()
        {
            do
            {
                _lastNotificationId = _lastNotificationId < FirstAutoNotificationId || _lastNotificationId == int.MaxValue
                    ? FirstAutoNotificationId
                    : _lastNotificationId + 1;
            }
            while (HasPendingNotificationId(_lastNotificationId));

            // OS 에 예약하기 전에 디스크에 확정한다. 백그라운드 전환 전에 앱이 죽어도 다음 실행에서 같은 ID 를 다시 쓰지 않게 한다
            PlayerPrefs.SetInt(LastIdPrefsKey, _lastNotificationId);
            PlayerPrefs.Save();

            return _lastNotificationId;
        }

        private bool HasPendingNotificationId(int notificationId)
        {
            for (var i = 0; i < PendingNotifications.Count; i++)
            {
                if (PendingNotifications[i].Notification.Id == notificationId)
                {
                    return true;
                }
            }

            return false;
        }

        // 권한 요청이 끝났으면 기다리던 콜백에 결과를 알린다
        private void UpdatePermissionRequest()
        {
            if (_permissionRequest == null || !_permissionRequest.IsDone)
            {
                return;
            }

            var result = _permissionRequest.Result;
            // 콜백 안에서 다시 요청할 수 있으므로 상태를 먼저 비우고 복사본으로 알린다
            var callbacks = _permissionCallbacks.ToArray();

            _permissionRequest = null;
            _permissionCallbacks.Clear();

            foreach (var callback in callbacks)
            {
                try
                {
                    callback(result);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// <see cref="_platform"/>에서 알림이 수신될 때 발생하는 이벤트입니다.
        /// </summary>
        private void OnNotificationReceived(IGameNotification deliveredNotification)
        {
            // 백그라운드 메시지는 무시 (Android에서 가끔 발생)
            if (!_inForeground)
            {
                return;
            }

            // 대기 목록에서 찾기
            int deliveredIndex = PendingNotifications.FindIndex(
                scheduledNotification => scheduledNotification.Notification.Id == deliveredNotification.Id);
            
            if (deliveredIndex >= 0)
            {
                // 구독자가 콜백에서 취소 API 를 불러 목록을 바꿀 수 있으므로 먼저 목록에서 뺀 뒤 알린다
                var deliveredPendingNotification = PendingNotifications[deliveredIndex];
                PendingNotifications.RemoveAt(deliveredIndex);
                OnLocalNotificationDelivered?.Invoke(deliveredPendingNotification);
            }
        }

        /// <summary>
        /// OS 에 이미 예약된 알림을 저장값에서 복원했을 때, 다시 예약하지 않고 예약됨 상태로 표시합니다.
        /// </summary>
        private static void MarkAsAlreadyScheduled(IGameNotification notification)
        {
#if UNITY_ANDROID
            if (notification is AndroidGameNotification androidNotification)
            {
                androidNotification.OnScheduled();
            }
#elif UNITY_IOS
            if (notification is iOSGameNotification iosNotification)
            {
                iosNotification.OnScheduled();
            }
#endif
        }

        // 포그라운드 알림을 지우고 파일에서 항목을 재예약
        private void OnForegrounding()
        {
            PendingNotifications.Clear();
            _platform.OnForeground();

            // 앱 안 알림 설정이 꺼져 있으면 OS 에 남은 예약을 지우고 저장된 목록을 되살리지 않는다
            if (!_notificationsEnabled)
            {
                _platform.CancelAllScheduledNotifications();

                return;
            }

            // 저장된 항목 역직렬화
            var notifications = SerializableNotificationConverter.FromJson(PlayerPrefs.GetString(NotificationsPrefsKey));

            // 포그라운드 전환
            if ((Mode & OperatingMode.ClearOnForegrounding) == OperatingMode.ClearOnForegrounding)
            {
                // 포그라운드 전환 시 지우기
                _platform.CancelAllScheduledNotifications();

                // 재예약 모드이고 로드된 항목이 있는 경우에만 재예약
                if (notifications == null || (Mode & OperatingMode.RescheduleAfterClearing) != OperatingMode.RescheduleAfterClearing)
                {
                    return;
                }

                // 역직렬화된 알림을 재예약
                foreach (var savedNotification in notifications)
                {
                    if (savedNotification.DeliveryTime > DateTime.Now)
                    {
                        var pendingNotification = ScheduleNotification(savedNotification.AsGameNotification(_platform));
                        
                        pendingNotification.Reschedule = true;
                    }
                }
            }
            else
            {
                // 역직렬화된 모든 항목에 대해 PendingNotification 래퍼를 생성.
                // 지워지지 않았으므로 재예약하지 않음
                if (notifications == null)
                {
                    return;
                }

                foreach (var savedNotification in notifications)
                {
                    if (savedNotification.DeliveryTime > DateTime.Now)
                    {
                        var restoredNotification = savedNotification.AsGameNotification(_platform);

                        // OS 예약은 그대로 남아 있으므로 다시 예약하지 않고 예약됨 상태만 되살린다.
                        // 그래야 다음 백그라운드 전환 때 저장 대상(Scheduled)에 다시 포함된다.
                        MarkAsAlreadyScheduled(restoredNotification);
                        PendingNotifications.Add(new PendingNotification(restoredNotification));
                    }
                }
            }
        }
    }
}

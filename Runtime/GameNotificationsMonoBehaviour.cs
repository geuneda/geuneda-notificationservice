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

        private IGameNotificationsPlatform _platform;
        private bool _inForeground = true;

        /// <summary>
        /// 예약되었거나 큐에 있는 알림 컬렉션을 가져옵니다.
        /// </summary>
        public List<PendingNotification> PendingNotifications { get; private set; } = new List<PendingNotification>();

        /// <summary>
        /// 이 관리자가 초기화되었는지 여부를 가져옵니다.
        /// </summary>
        public bool Initialized { get; private set; }

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
        }

        /// <summary>
        /// 큐 모드일 때 대기 목록에서 만료된 알림을 확인합니다.
        /// </summary>
        private void Update()
        {
            if ((Mode & OperatingMode.Queue) != OperatingMode.Queue)
            {
                return;
            }

            // 각 대기 중인 알림의 만료 여부를 확인한 후 제거
            for (int i = PendingNotifications.Count - 1; i >= 0; --i)
            {
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
        /// 애플리케이션 포그라운드/백그라운드 이벤트에 응답합니다.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (_platform == null || !Initialized)
            {
                return;
            }

            _inForeground = hasFocus;

            if (hasFocus)
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

            // 디스크에 저장
            PlayerPrefs.SetString("notifications", JsonUtility.ToJson(notificationsToSave));
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

            Initialized = true;

#if UNITY_ANDROID
            _platform = new AndroidNotificationsPlatform();

            // 알림 채널 등록
            var doneDefault = false;
            foreach (var notificationChannel in channels)
            {
                var platform = _platform as AndroidNotificationsPlatform;
                
                if (!doneDefault)
                {
                    doneDefault = true;
                    platform.DefaultChannelId = notificationChannel.Id;
                }

                platform.RegisterChannel(notificationChannel);
            }
#elif UNITY_IOS
            _platform = new iOSNotificationsPlatform();
#endif

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
        public PendingNotification ScheduleNotification(IGameNotification notification)
        {
            if (!Initialized)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }

            if (notification == null)
            {
                return null;
            }

            // 큐 모드이면 즉시 예약하지 않음.
            // 또한 시간 기반이 아닌 전달은 즉시 예약 (iOS용)
            if ((Mode & OperatingMode.Queue) != OperatingMode.Queue || notification.DeliveryTime == null)
            {
                _platform?.ScheduleNotification(notification);
            }
            else if (!notification.Id.HasValue)
            {
                // ID가 없는 항목에 대해 ID를 생성 (나중에 식별할 수 있도록)
                notification.Id = Math.Abs(DateTime.Now.ToString("yyMMddHHmmssffffff").GetHashCode());
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
                OnLocalNotificationDelivered?.Invoke(PendingNotifications[deliveredIndex]);
                PendingNotifications.RemoveAt(deliveredIndex);
            }
        }

        // 포그라운드 알림을 지우고 파일에서 항목을 재예약
        private void OnForegrounding()
        {
            PendingNotifications.Clear();
            _platform.OnForeground();

            // 저장된 항목 역직렬화
            var notifications = JsonUtility.FromJson<List<SerializableNotification>>(PlayerPrefs.GetString("notifications"));

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
                        PendingNotifications.Add(new PendingNotification(savedNotification.AsGameNotification(_platform)));
                    }
                }
            }
        }
    }
}

using System;
using System.Linq;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// 알림 채널을 나타내는 크로스 플랫폼 래퍼.
    /// </summary>
    /// <remarks>
    /// <para>Android에서는 Android 알림 채널에 거의 직접 매핑됩니다. iOS에서는 아무 작업도 수행하지 않습니다.</para>
    /// <para>Android를 대상으로 하는 프로젝트에서는 최소한 하나의 채널이 필요합니다.</para>
    /// </remarks>
    public readonly struct GameNotificationChannel
    {
        /// <summary>
        /// 이 채널에 표시되는 알림 스타일. Android 알림의 중요도 설정에 해당하며,
        /// iOS에서는 아무 작업도 수행하지 않습니다.
        /// </summary>
        public enum NotificationStyle
        {
            /// <summary>
            /// 알림이 상태 표시줄에 나타나지 않습니다.
            /// </summary>
            None = 0,
            /// <summary>
            /// 알림이 소리를 내지 않습니다.
            /// </summary>
            NoSound = 2,
            /// <summary>
            /// 알림이 소리를 재생합니다.
            /// </summary>
            Default = 3,
            /// <summary>
            /// 알림이 헤드업 팝업도 표시합니다.
            /// </summary>
            Popup = 4
        }

        /// <summary>
        /// 기기 잠금 화면에서 알림이 표시되는 방식을 제어합니다.
        /// </summary>
        public enum PrivacyMode
        {
            /// <summary>
            /// 보안 잠금 화면에서 알림이 표시되지 않습니다.
            /// </summary>
            Secret = -1,
            /// <summary>
            /// 알림이 아이콘을 표시하지만, 보안 잠금 화면에서는 내용이 숨겨집니다.
            /// </summary>
            Private = 0,
            /// <summary>
            /// 알림이 모든 잠금 화면에 표시됩니다.
            /// </summary>
            Public
        }

        /// <summary>
        /// 채널의 식별자.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// 사용자에게 표시되는 채널 이름.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// 사용자에게 표시되는 채널 설명.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// 이 채널의 메시지가 배지를 표시할 수 있는지 여부를 결정하는 플래그. 기본값은 true.
        /// </summary>
        public readonly bool ShowsBadge;

        /// <summary>
        /// 이 채널의 메시지가 기기 조명을 깜빡이게 하는지 여부를 결정하는 플래그. 기본값은 false.
        /// </summary>
        public readonly bool ShowLights;

        /// <summary>
        /// 이 채널의 메시지가 기기를 진동시키는지 여부를 결정하는 플래그. 기본값은 true.
        /// </summary>
        public readonly bool Vibrates;

        /// <summary>
        /// 이 채널의 메시지가 방해 금지 설정을 우회하는지 여부를 결정하는 플래그. 기본값은 false.
        /// </summary>
        public readonly bool HighPriority;

        /// <summary>
        /// 이 알림의 표시 스타일. 기본값은 <see cref="NotificationStyle.Popup"/>.
        /// </summary>
        public readonly NotificationStyle Style;

        /// <summary>
        /// 이 알림의 개인정보 설정. 기본값은 <see cref="PrivacyMode.Public"/>.
        /// </summary>
        public readonly PrivacyMode Privacy;

        /// <summary>
        /// 이 채널의 사용자 정의 진동 패턴. 기본값을 사용하려면 null로 설정.
        /// </summary>
        public readonly int[] VibrationPattern;

        /// <summary>
        /// 선택적 필드를 기본값으로 설정하여 <see cref="GameNotificationChannel"/>의
        /// 새 인스턴스를 초기화합니다.
        /// </summary>
        public GameNotificationChannel(string id, string name, string description) : this()
        {
            Id = id;
            Name = name;
            Description = description;

            ShowsBadge = true;
            ShowLights = false;
            Vibrates = true;
            HighPriority = false;
            Style = NotificationStyle.Popup;
            Privacy = PrivacyMode.Public;
            VibrationPattern = null;
        }

        /// <summary>
        /// 알림 스타일과 선택적으로 기타 모든 설정을 제공하여 <see cref="GameNotificationChannel"/>의
        /// 새 인스턴스를 초기화합니다.
        /// </summary>
        public GameNotificationChannel(string id, string name, string description, NotificationStyle style, bool showsBadge = true, bool showLights = false, bool vibrates = true, bool highPriority = false, PrivacyMode privacy = PrivacyMode.Public, long[] vibrationPattern = null)
        {
            Id = id;
            Name = name;
            Description = description;
            ShowsBadge = showsBadge;
            ShowLights = showLights;
            Vibrates = vibrates;
            HighPriority = highPriority;
            Style = style;
            Privacy = privacy;
            if (vibrationPattern != null)
                VibrationPattern = vibrationPattern.Select(v => (int)v).ToArray();
            else
                VibrationPattern = null;
        }
    }
}

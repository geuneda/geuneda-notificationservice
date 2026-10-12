using System;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// 이 애플리케이션에 전달될 알림을 나타냅니다.
    /// </summary>
    public interface IGameNotification
    {
        /// <summary>
        /// 이 알림의 고유 식별자를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// <para>
        /// null인 경우 생성 방식은 플랫폼마다 다릅니다. Android는 예약할 때 OS가 발급한 ID로 채우고,
        /// iOS는 알림 객체를 만들 때 내부 식별자가 생성됩니다. <see cref="OperatingMode.Queue"/> 모드에서
        /// 큐에 넣는 알림과 에디터에서 예약하는 알림은 예약 요청 시 ID가 생성됩니다.
        /// </para>
        /// <para>일부 플랫폼에서는 내부적으로 문자열 식별자로 변환될 수 있습니다.</para>
        /// </remarks>
        /// <value>이 알림의 고유 정수 식별자, 또는 명시적으로 설정되지 않은 경우 (일부 플랫폼에서) null.</value>
        int? Id { get; set; }

        /// <summary>
        /// 알림의 제목을 가져오거나 설정합니다.
        /// </summary>
        /// <value>알림의 제목 메시지.</value>
        string Title { get; set; }

        /// <summary>
        /// 알림의 본문 텍스트를 가져오거나 설정합니다.
        /// </summary>
        /// <value>알림의 본문 메시지.</value>
        string Body { get; set; }

        /// <summary>
        /// 알림의 부제목을 가져오거나 설정합니다.
        /// </summary>
        /// <value>알림의 부제목 메시지.</value>
        string Subtitle { get; set; }

        /// <summary>
        /// 이 알림이 속한 채널을 가져오거나 설정합니다.
        /// </summary>
        /// <value>알림 채널의 플랫폼별 문자열 식별자.</value>
        string Channel { get; set; }

        /// <summary>
        /// 이 알림의 배지 번호를 가져오거나 설정합니다. null이면 배지 번호가 표시되지 않습니다.
        /// </summary>
        /// <value>앱 배지에 표시되는 숫자.</value>
        int? BadgeNumber { get; set; }

        /// <summary>
        /// 사용자가 탭할 때 이 알림이 자동으로 닫히는지 여부를 가져오거나 설정합니다.
        /// Android에서만 사용 가능합니다.
        /// </summary>
        bool ShouldAutoCancel { get; set; }

        /// <summary>
        /// 알림 전달 시간을 가져오거나 설정합니다.
        /// </summary>
        /// <value>로컬 시간 기준의 전달 시간.</value>
        DateTime? DeliveryTime { get; set; }

        /// <summary>
        /// 이 알림이 예약되었는지 여부를 가져옵니다.
        /// </summary>
        /// <value>기본 운영 체제에 알림이 예약된 경우 true.</value>
        bool Scheduled { get; }

        /// <summary>
        /// 알림 소형 아이콘.
        /// </summary>
        string SmallIcon { get; set; }

        /// <summary>
        /// 알림 대형 아이콘.
        /// </summary>
        string LargeIcon { get; set; }
    }
}

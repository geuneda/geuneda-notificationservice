// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
	/// <summary>
	/// OS 알림 권한 상태입니다. 앱 안 알림 설정(<see cref="INotificationService.IsEnabled"/>)과 함께 알림 표시의 두 번째 게이트입니다.
	/// </summary>
	public enum NotificationPermission
	{
		/// <summary>
		/// 아직 사용자에게 묻지 않았습니다.
		/// </summary>
		NotDetermined,

		/// <summary>
		/// 권한 요청 창이 떠 있어 사용자의 응답을 기다리는 중입니다.
		/// </summary>
		Pending,

		/// <summary>
		/// 알림을 표시할 수 있습니다.
		/// </summary>
		Granted,

		/// <summary>
		/// 사용자가 거부했거나 기기 설정에서 이 앱의 알림을 껐습니다.
		/// </summary>
		Denied
	}
}

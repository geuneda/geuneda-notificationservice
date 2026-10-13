using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.NotificationService.Tests
{
	/// <summary>
	/// 테스트가 만든 관리자 GameObject 와 PlayerPrefs 키를 정리합니다.
	/// </summary>
	internal static class NotificationTestUtils
	{
		public static void DestroyAllManagers()
		{
			// FindObjectsByType 은 Unity 2022.2 부터 있으므로 패키지 최소 버전(2021.3)에서는 이전 API 를 쓴다
#if UNITY_2022_2_OR_NEWER
			var managers = Object.FindObjectsByType<GameNotificationsMonoBehaviour>(FindObjectsSortMode.None);
#else
			var managers = Object.FindObjectsOfType<GameNotificationsMonoBehaviour>();
#endif

			foreach (var manager in managers)
			{
				Object.DestroyImmediate(manager.gameObject);
			}
		}

		public static void DeletePrefs()
		{
			PlayerPrefs.DeleteKey(GameNotificationsMonoBehaviour.EnabledPrefsKey);
			PlayerPrefs.DeleteKey(GameNotificationsMonoBehaviour.LastIdPrefsKey);
			PlayerPrefs.DeleteKey(GameNotificationsMonoBehaviour.NotificationsPrefsKey);
		}
	}
}

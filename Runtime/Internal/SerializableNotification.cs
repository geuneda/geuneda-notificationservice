using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
	/// <summary>
	/// 게임이 포그라운드로 전환될 때 디스크에 직렬화/역직렬화할 알림
	/// </summary>
	[Serializable]
	internal struct SerializableNotification
	{
		public int? Id;
		public string Title;
		public string Body;
		public string Subtitle;
		public string Channel;
		public int? BadgeNumber;
		public DateTime? DeliveryTime;
		public string SmallIcon;
		public string LargeIcon;
		public bool ShouldAutoCancel;
	}

	/// <summary>
	/// <see cref="JsonUtility"/>로 저장하기 위한 알림 목록 래퍼.
	/// <see cref="JsonUtility"/>는 최상위 리스트를 직렬화하지 못하므로 객체로 감쌉니다.
	/// </summary>
	[Serializable]
	internal class SerializableNotificationCollection
	{
		public List<SerializableNotificationData> Notifications = new List<SerializableNotificationData>();
	}

	/// <summary>
	/// <see cref="JsonUtility"/>로 저장하기 위한 <see cref="SerializableNotification"/>의 형태.
	/// nullable 타입과 <see cref="DateTime"/>은 직렬화되지 않으므로 값 존재 여부와 값을 나누어 저장합니다.
	/// </summary>
	[Serializable]
	internal class SerializableNotificationData
	{
		public bool HasId;
		public int Id;
		public string Title;
		public string Body;
		public string Subtitle;
		public string Channel;
		public bool HasBadgeNumber;
		public int BadgeNumber;
		public bool HasDeliveryTime;
		// DateTime.ToBinary 값
		public long DeliveryTime;
		// 아래 필드가 없는 이전 형식 값은 기본값(null, false)으로 읽힌다
		public string SmallIcon;
		public string LargeIcon;
		public bool ShouldAutoCancel;
	}

	/// <summary>
	/// 직렬화 변환 클래스
	/// </summary>
	internal static class SerializableNotificationConverter
	{
		/// <summary>
		/// 알림 목록을 <see cref="JsonUtility"/> JSON 문자열로 변환합니다.
		/// </summary>
		public static string ToJson(IEnumerable<SerializableNotification> notifications)
		{
			var collection = new SerializableNotificationCollection();

			foreach (var notification in notifications)
			{
				collection.Notifications.Add(new SerializableNotificationData
				{
					HasId = notification.Id.HasValue,
					Id = notification.Id.GetValueOrDefault(),
					Title = notification.Title,
					Body = notification.Body,
					Subtitle = notification.Subtitle,
					Channel = notification.Channel,
					HasBadgeNumber = notification.BadgeNumber.HasValue,
					BadgeNumber = notification.BadgeNumber.GetValueOrDefault(),
					HasDeliveryTime = notification.DeliveryTime.HasValue,
					DeliveryTime = notification.DeliveryTime.GetValueOrDefault().ToBinary(),
					SmallIcon = notification.SmallIcon,
					LargeIcon = notification.LargeIcon,
					ShouldAutoCancel = notification.ShouldAutoCancel,
				});
			}

			return JsonUtility.ToJson(collection);
		}

		/// <summary>
		/// <see cref="ToJson"/>으로 만든 문자열을 알림 목록으로 변환합니다.
		/// 비어 있거나 읽을 수 없는 문자열(이전 버전이 저장한 "{}" 등)이면 빈 목록을 반환합니다.
		/// </summary>
		public static List<SerializableNotification> FromJson(string json)
		{
			var result = new List<SerializableNotification>();

			if (string.IsNullOrEmpty(json))
			{
				return result;
			}

			try
			{
				var collection = JsonUtility.FromJson<SerializableNotificationCollection>(json);

				if (collection?.Notifications == null)
				{
					return result;
				}

				foreach (var data in collection.Notifications)
				{
					if (data == null)
					{
						continue;
					}

					result.Add(new SerializableNotification
					{
						Id = data.HasId ? data.Id : (int?)null,
						Title = data.Title,
						Body = data.Body,
						Subtitle = data.Subtitle,
						Channel = data.Channel,
						BadgeNumber = data.HasBadgeNumber ? data.BadgeNumber : (int?)null,
						DeliveryTime = data.HasDeliveryTime ? DateTime.FromBinary(data.DeliveryTime) : (DateTime?)null,
						SmallIcon = data.SmallIcon,
						LargeIcon = data.LargeIcon,
						ShouldAutoCancel = data.ShouldAutoCancel,
					});
				}
			}
			catch (ArgumentException)
			{
				// 형식이 맞지 않는 JSON이나 잘못된 시간 값은 저장된 알림이 없는 것으로 처리
				result.Clear();
			}

			return result;
		}

		public static IGameNotification AsGameNotification(this SerializableNotification serializableNotification,
			IGameNotificationsPlatform platform)
		{
			var notification = platform.CreateNotification();

			// iOS는 null ID를 허용하지 않으므로 값이 있을 때만 설정
			if (serializableNotification.Id.HasValue)
			{
				notification.Id = serializableNotification.Id;
			}

			notification.Title = serializableNotification.Title;
			notification.Body = serializableNotification.Body;
			notification.Subtitle = serializableNotification.Subtitle;
			notification.Channel = serializableNotification.Channel;
			notification.BadgeNumber = serializableNotification.BadgeNumber;
			notification.DeliveryTime = serializableNotification.DeliveryTime;
			notification.SmallIcon = serializableNotification.SmallIcon;
			notification.LargeIcon = serializableNotification.LargeIcon;
			notification.ShouldAutoCancel = serializableNotification.ShouldAutoCancel;

			return notification;
		}
        
		public static SerializableNotification AsSerializableNotification(this PendingNotification pendingNotification)
		{
			return new SerializableNotification
			{
				Id = pendingNotification.Notification.Id,
				Title = pendingNotification.Notification.Title,
				Body = pendingNotification.Notification.Body,
				Subtitle = pendingNotification.Notification.Subtitle,
				Channel = pendingNotification.Notification.Channel,
				BadgeNumber = pendingNotification.Notification.BadgeNumber,
				DeliveryTime = pendingNotification.Notification.DeliveryTime,
				SmallIcon = pendingNotification.Notification.SmallIcon,
				LargeIcon = pendingNotification.Notification.LargeIcon,
				ShouldAutoCancel = pendingNotification.Notification.ShouldAutoCancel,
			};
		}
	}

}
using System;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
	/// <summary>
	/// <see cref="IGameNotification"/>의 에디터 전용 구현.
	/// </summary>
	internal class EditorGameNotification : IGameNotification
	{
		/// <inheritdoc />
		public int? Id { get; set; }
		/// <inheritdoc />
		public string Title { get; set; }
		/// <inheritdoc />
		public string Body { get; set; }
		/// <inheritdoc />
		public string Subtitle { get; set; }
		/// <inheritdoc />
		public string Channel { get; set; }
		/// <inheritdoc />
		public int? BadgeNumber { get; set; }
		/// <inheritdoc />
		public bool ShouldAutoCancel { get; set; }
		/// <inheritdoc />
		public DateTime? DeliveryTime { get; set; }
		/// <inheritdoc />
		public bool Scheduled { get; }
		/// <inheritdoc />
		public string SmallIcon { get; set; }
		/// <inheritdoc />
		public string LargeIcon { get; set; }
	}
}
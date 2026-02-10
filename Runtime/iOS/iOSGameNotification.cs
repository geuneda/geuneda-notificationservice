#if UNITY_IOS
using System;
using Unity.Notifications.iOS;
using UnityEngine;
using UnityEngine.Assertions;

// ReSharper disable once CheckNamespace

namespace Geuneda.NotificationService
{
    /// <summary>
    /// <see cref="IGameNotification"/>의 iOS 구현.
    /// </summary>
    public class iOSGameNotification : IGameNotification
    {
        private readonly iOSNotification internalNotification;

        /// <summary>
        /// 모바일 알림 시스템에서 사용하는 내부 알림 객체를 가져옵니다.
        /// </summary>
        public iOSNotification InternalNotification => internalNotification;

        /// <inheritdoc />
        /// <remarks>
        /// 내부적으로 문자열로 저장됩니다. 조회 시 정수로 파싱됩니다.
        /// </remarks>
        /// <value>정수 형태의 식별자, 또는 식별자를 숫자로 파싱할 수 없는 경우 null.</value>
        public int? Id
        {
            get
            {
                if (!int.TryParse(internalNotification.Identifier, out int value))
                {
                    Debug.LogWarning("Internal iOS notification's identifier isn't a number.");
                    return null;
                }

                return value;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                internalNotification.Identifier = value.Value.ToString();
            }
        }

        /// <inheritdoc />
        public string Title { get => internalNotification.Title; set => internalNotification.Title = value; }

        /// <inheritdoc />
        public string Body { get => internalNotification.Body; set => internalNotification.Body = value; }

        /// <inheritdoc />
        public string Subtitle { get => internalNotification.Subtitle; set => internalNotification.Subtitle = value; }

        /// <inheritdoc />
        /// <remarks>
        /// iOS에서는 알림의 카테고리 식별자를 나타냅니다.
        /// </remarks>
        /// <value><see cref="CategoryIdentifier"/>의 값.</value>
        public string Channel { get => CategoryIdentifier; set => CategoryIdentifier = value; }

        /// <inheritdoc />
        public int? BadgeNumber
        {
            get => internalNotification.Badge != -1 ? internalNotification.Badge : (int?)null;
            set => internalNotification.Badge = value ?? -1;
        }

        /// <inheritdoc />
        public bool ShouldAutoCancel { get; set; }

        /// <inheritdoc />
        public bool Scheduled { get; private set; }

        /// <inheritdoc />
        /// <remarks>
        /// <para>iOS에서 이 값을 설정하면 알림이 캘린더 시간에 전달됩니다.</para>
        /// <para>이전에 다른 유형의 트리거로 수동 설정되었거나 설정된 적이 없는 경우
        /// null을 반환합니다.</para>
        /// <para>제공된 DateTime의 밀리초 구성 요소는 무시됩니다.</para>
        /// </remarks>
        /// <value>이 메시지의 전달 시간을 나타내는 <see cref="DateTime"/>, 또는 설정되지 않았거나
        /// 트리거가 <see cref="iOSNotificationCalendarTrigger"/>가 아닌 경우 null.</value>
        public DateTime? DeliveryTime
        {
            get
            {
                if (!(internalNotification.Trigger is iOSNotificationCalendarTrigger calendarTrigger))
                {
                    return null;
                }

                DateTime now = DateTime.Now;
                var result = new DateTime
                    (
                    calendarTrigger.Year ?? now.Year,
                    calendarTrigger.Month ?? now.Month,
                    calendarTrigger.Day ?? now.Day,
                    calendarTrigger.Hour ?? now.Hour,
                    calendarTrigger.Minute ?? now.Minute,
                    calendarTrigger.Second ?? now.Second,
                    DateTimeKind.Local
                    );

                return result;
            }
            set
            {
                if (!value.HasValue)
                {
                    return;
                }

                DateTime date = value.Value.ToLocalTime();

                internalNotification.Trigger = new iOSNotificationCalendarTrigger
                {
                    Year = date.Year,
                    Month = date.Month,
                    Day = date.Day,
                    Hour = date.Hour,
                    Minute = date.Minute,
                    Second = date.Second
                };
            }
        }

        /// <summary>
        /// 이 알림의 카테고리 식별자.
        /// </summary>
        public string CategoryIdentifier
        {
            get => internalNotification.CategoryIdentifier;
            set => internalNotification.CategoryIdentifier = value;
        }

        /// <summary>
        /// iOS에서는 아무 작업도 수행하지 않습니다.
        /// </summary>
        public string SmallIcon { get => null; set {} }

        /// <summary>
        /// iOS에서는 아무 작업도 수행하지 않습니다.
        /// </summary>
        public string LargeIcon { get => null; set {} }

        /// <summary>
        /// <see cref="iOSGameNotification"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        public iOSGameNotification()
        {
            internalNotification = new iOSNotification
            {
                ShowInForeground = true // 기본적으로 포그라운드에서 전달
            };
        }

        /// <summary>
        /// 전달된 알림으로부터 <see cref="iOSGameNotification"/>의 새 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="internalNotification">전달된 알림.</param>
        internal iOSGameNotification(iOSNotification internalNotification)
        {
            this.internalNotification = internalNotification;
        }

        /// <summary>
        /// 이 알림의 예약 완료 플래그를 설정합니다.
        /// </summary>
        internal void OnScheduled()
        {
            Assert.IsFalse(Scheduled);
            Scheduled = true;
        }
    }
}
#endif

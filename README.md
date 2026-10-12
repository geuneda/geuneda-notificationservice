# Geuneda Notification Service

Unity Mobile Notifications 패키지(`com.unity.mobile.notifications`)를 감싸 Android/iOS 로컬 알림의 예약, 취소, 닫기를 하나의 API(`INotificationService`)로 다루는 패키지입니다.

## 설치

Unity 에디터에서 `Window > Package Manager > + > Add package from git URL...` 을 선택하고 아래 URL 을 입력합니다.

```
https://github.com/geuneda/geuneda-notificationservice.git
```

또는 `Packages/manifest.json` 에 직접 추가합니다.

```json
{
  "dependencies": {
    "com.geuneda.notificationservice": "https://github.com/geuneda/geuneda-notificationservice.git"
  }
}
```

특정 버전에 고정하려면 URL 끝에 `#<태그>` 를 붙입니다. 사용할 수 있는 태그는 GitHub 레포에서 확인하세요.

## 빠른 시작

```csharp
using System;
using Geuneda.NotificationService;
using UnityEngine;

public class NotificationExample : MonoBehaviour
{
    private const int DailyRewardId = 1001;

    private INotificationService _notifications;

    private void Start()
    {
        // Android 에서는 채널이 최소 하나 필요합니다. 첫 번째 채널이 기본 채널이 됩니다.
        _notifications = new MobileNotificationService(
            new GameNotificationChannel("default", "기본 알림", "게임 일반 알림"));

        var notification = _notifications.CreateNotification();
        if (notification == null)
        {
            return; // Android/iOS 가 아닌 플랫폼
        }

        notification.Id = DailyRewardId;
        notification.Title = "보상 준비 완료";
        notification.Body = "접속해서 보상을 받으세요.";
        notification.DeliveryTime = DateTime.Now.AddHours(4);

        _notifications.ScheduleNotification(notification);
    }

    public void CancelDailyReward()
    {
        _notifications.CancelNotification(DailyRewardId);
    }
}
```

`MobileNotificationService` 는 생성될 때 `DontDestroyOnLoad` 가 적용된 `NotificationService` GameObject 를 만들고 그 위의 `GameNotificationsMonoBehaviour` 를 초기화합니다. 앱 전체에서 한 번만 생성하세요.

## 핵심 개념

| 타입 | 역할 |
|------|------|
| `INotificationService` / `MobileNotificationService` | 알림 생성, 예약, 취소, 닫기를 위한 진입점 |
| `IGameNotification` | 알림 데이터(`Id`, `Title`, `Body`, `Subtitle`, `Channel`, `BadgeNumber`, `ShouldAutoCancel`, `DeliveryTime`, `SmallIcon`, `LargeIcon`, 읽기 전용 `Scheduled`) |
| `PendingNotification` | 예약되었거나 큐에 있는 알림의 래퍼(`Notification` 필드, `Reschedule` 필드) |
| `GameNotificationChannel` | Android 알림 채널 설정(`readonly struct`). iOS 에서는 아무 동작도 하지 않습니다 |
| `GameNotificationsMonoBehaviour` | 플랫폼별 구현을 감싸고 포그라운드/백그라운드 전환을 처리하는 MonoBehaviour |
| `OperatingMode` | `GameNotificationsMonoBehaviour.Mode` 와 `MobileNotificationService` 생성자에 쓰는 `[Flags]` 열거형 |

### INotificationService 메서드

| 멤버 | 설명 |
|------|------|
| `CreateNotification()` | 현재 플랫폼에 맞는 `IGameNotification` 을 만듭니다 |
| `ScheduleNotification(IGameNotification)` | 알림을 예약하고 `PendingNotification` 을 반환합니다 |
| `CancelNotification(int)` / `CancelAllScheduledNotifications()` | 예약된 알림을 취소합니다 |
| `DismissNotification(int)` / `DismissAllDisplayedNotifications()` | 이미 표시된 알림을 닫습니다 |
| `PendingNotifications` | 예약되었거나 큐에 있는 알림 목록 |
| `OnLocalNotificationDeliveredEvent` / `OnLocalNotificationExpiredEvent` | 앱이 포그라운드일 때 대기 목록의 알림이 전달되면 / `Queue` 모드에서 큐 알림이 표시 시점을 지나 제거되면 발생 |

### OperatingMode

| 값 | 동작 |
|----|------|
| `NoQueue` (기본값) | 예약 즉시 OS 에 전달합니다 |
| `Queue` | 앱이 백그라운드로 갈 때까지 큐에 보관하고, 배지 번호가 하나도 설정되지 않았으면 전달 시간순으로 자동 부여합니다(`AutoBadging`) |
| `ClearOnForegrounding` | 포그라운드로 돌아올 때 예약된 알림을 모두 취소합니다 |
| `RescheduleAfterClearing` | 취소 후 `Reschedule = true` 인 미래 알림을 다시 예약합니다. `ClearOnForegrounding` 과 함께 쓸 때만 유효합니다 |
| `QueueAndClear` | `Queue` + `ClearOnForegrounding` |
| `QueueClearAndReschedule` | `Queue` + `ClearOnForegrounding` + `RescheduleAfterClearing`. 앱이 포그라운드에 있는 동안 알림이 표시되지 않게 합니다 |

`new MobileNotificationService(channels)` 는 `NoQueue` 로 동작합니다. 다른 모드가 필요하면 첫 인자로 모드를 넘깁니다. 모드는 생성할 때만 정할 수 있습니다.

```csharp
var notifications = new MobileNotificationService(
    OperatingMode.QueueClearAndReschedule,
    new GameNotificationChannel("default", "기본 알림", "게임 일반 알림"));
```

### 플랫폼별 차이

| 항목 | Android | iOS |
|------|---------|-----|
| `Channel` | 알림 채널 ID. 지정하지 않으면 첫 번째로 등록한 채널 | `CategoryIdentifier` |
| `Subtitle` | 무시됨 | 지원 |
| `ShouldAutoCancel` | 지원 | 효과 없음 |
| `SmallIcon` / `LargeIcon` | 지원 | 무시됨 |
| `Id` 미지정 시 | 예약할 때 OS 가 발급한 ID 가 채워짐 | 정수로 파싱할 수 없으면 `null` 반환(경고 로그) |
| `DeliveryTime` | `null` 을 넣으면 `ArgumentNullException` | 캘린더 트리거로 변환되며 밀리초는 무시됨 |
| 포그라운드 전환 | 추가 동작 없음 | 앱 배지를 0 으로 초기화 |

## 주의 사항

- **에디터에서는 실제로 예약되지 않습니다.** `MobileNotificationService` 는 에디터에서 내부 전용 `EditorGameNotification` 을 반환하고, `ScheduleNotification` 은 ID 만 채운 `PendingNotification` 을 돌려줄 뿐 `PendingNotifications` 에 추가하지 않습니다. 따라서 에디터에서는 전달/만료 이벤트도 발생하지 않습니다.
- `MobileNotificationService.ScheduleNotification(null)` 은 `ArgumentNullException` 을 던집니다.
- **지원하지 않는 플랫폼**(Android/iOS 외 빌드)에서는 `CreateNotification()` 이 `null` 을 반환하고 취소/닫기 메서드는 아무 동작도 하지 않습니다.
- **알림 권한을 요청하지 않습니다.** iOS 알림 권한이나 Android 13 이상의 알림 권한은 Unity Mobile Notifications 의 API 로 직접 요청해야 합니다.
- **원격(푸시) 알림은 다루지 않습니다.** 수신 처리는 로컬 알림에 대해서만 구현되어 있습니다.
- 취소할 알림은 `Id` 를 직접 지정해 두는 것이 안전합니다. 플랫폼에 따라 자동 발급 여부가 다릅니다.
- 앱이 백그라운드로 갈 때 예약 상태를 `PlayerPrefs` 키 `notifications` 에 저장하고, 포그라운드로 돌아오면 다시 읽습니다. 같은 키를 다른 용도로 쓰지 마세요. 저장되는 값은 `Id`, `Title`, `Body`, `Subtitle`, `Channel`, `BadgeNumber`, `DeliveryTime`, `SmallIcon`, `LargeIcon`, `ShouldAutoCancel` 입니다. 예약을 지우지 않는 모드에서는 OS 예약이 남아 있으므로 복원한 알림을 다시 예약하지 않고 예약됨(`Scheduled`) 상태로 되살립니다. 0.1.7 이하가 저장한 값은 읽을 수 없어 빈 상태로 처리됩니다.
- `AndroidGameNotification` 은 `UNITY_ANDROID`, `iOSGameNotification` 은 `UNITY_IOS` 에서만 컴파일됩니다. 플랫폼 타입을 직접 참조하는 코드는 같은 전처리 지시문으로 감싸야 합니다.

## 요구 사항과 의존성

- Unity 2019.4 이상 (`package.json` 의 `unity` 필드)
- `com.unity.mobile.notifications` 1.3.0 (`package.json` 에 선언된 버전)
- 어셈블리: `Geuneda.NotificationService` (네임스페이스 `Geuneda.NotificationService`)

변경 이력은 [CHANGELOG.md](CHANGELOG.md) 를 참고하세요.

## 라이선스

MIT. [LICENSE.md](LICENSE.md) 를 참고하세요. 원저작자는 Miguel Tomás(GameLovers)입니다.

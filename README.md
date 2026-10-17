# Geuneda Notification Service

Unity Mobile Notifications 패키지(`com.unity.mobile.notifications`)를 감싸 Android/iOS 로컬 알림의 예약, 취소, 닫기를 하나의 API(`INotificationService`)로 다루는 패키지입니다.

## 설치

Unity 에디터에서 `Window > Package Manager > + > Add package from git URL...` 을 선택하고 아래 URL 을 입력합니다.

```
https://github.com/geuneda/geuneda-notificationservice.git
```

또는 `Packages/manifest.json` 에 직접 추가합니다. core 는 Unity 레지스트리에 없어 자동으로 설치되지 않으므로 함께 추가합니다.

```json
{
  "dependencies": {
    "com.geuneda.core": "https://github.com/geuneda/geuneda-core.git",
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
| `NotificationPermission` | OS 알림 권한 상태(`NotDetermined`, `Pending`, `Granted`, `Denied`) |

### INotificationService 메서드

| 멤버 | 설명 |
|------|------|
| `CreateNotification()` | 현재 플랫폼에 맞는 `IGameNotification` 을 만듭니다 |
| `ScheduleNotification(IGameNotification)` | 알림을 예약하고 `PendingNotification` 을 반환합니다 |
| `CancelNotification(int)` / `CancelAllScheduledNotifications()` | 예약된 알림을 취소합니다 |
| `DismissNotification(int)` / `DismissAllDisplayedNotifications()` | 이미 표시된 알림을 닫습니다 |
| `PendingNotifications` | 예약되었거나 큐에 있는 알림 목록 |
| `IsEnabled` / `OnEnabledChanged` | 앱 안 알림 설정(기본값 `true`, `PlayerPrefs` 에 저장)과 그 값이 바뀔 때의 이벤트. 아래 "알림 설정과 OS 권한" 참고 |
| `Permission` / `RequestPermission(Action<NotificationPermission>)` | 현재 OS 알림 권한 상태와 권한 요청 |
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

배지 자동 부여는 `MobileNotificationService.AutoBadging`(기본값 `true`, 0.1.9)으로 끄고 켭니다. `INotificationService` 인터페이스가 아니라 구체 클래스의 프로퍼티입니다. `Queue` 가 들어간 모드에서, 큐의 알림 중 배지 번호를 직접 지정한 것이 하나도 없을 때 전달 시간순으로 1부터 매깁니다.

```csharp
notifications.AutoBadging = false; // 배지를 직접 관리할 때
```

### 알림 설정과 OS 권한 (2단 게이트)

알림은 두 게이트를 모두 통과해야 사용자에게 보입니다.

1. **앱 안 알림 설정(`IsEnabled`)**: 서비스가 지킵니다. 게임 설정 화면의 토글에 연결합니다. 값은 `PlayerPrefs` 에 저장되어 다음 실행에도 유지됩니다(기본값 `true`).
   - `false` 로 바꾸면 예약된 알림을 모두 취소하고 표시된 알림을 닫으며, 저장해 둔 예약 목록도 지웁니다. 이후 `ScheduleNotification` 은 예약하지 않고 `null` 을 반환합니다.
   - `true` 로 바꿔도 지운 알림을 되살리지 않습니다. `OnEnabledChanged` 에서 필요한 알림을 다시 예약하세요. 게임은 "켜질 때(와 시작할 때) 필요한 알림을 모두 예약"만 하면 됩니다.
2. **OS 알림 권한(`Permission`)**: OS 가 지킵니다. `RequestPermission` 으로 요청합니다. 사용자가 기기 설정에서 바꿀 수 있으므로 필요할 때마다 `Permission` 을 다시 읽습니다.
   - Android 13 이상(targetSdk 33 이상)에서는 `POST_NOTIFICATIONS` 권한 요청 창을, iOS 에서는 알림·배지·소리 권한 요청 창을 띄웁니다. 이미 결정되어 창이 뜨지 않으면 바로 결과를 알립니다. 요청 창에 사용자가 응답하기 전에는 `Permission` 이 `Pending` 입니다.
   - **iOS 는 Unity Mobile Notifications 의 기본 설정이 앱 시작 때 권한 창을 띄웁니다.** 요청 시점을 `RequestPermission` 으로 정하려면 Project Settings > Mobile Notifications > iOS 에서 `Request Authorization on App Launch` 를 끄세요. 켜 두면 `RequestPermission` 은 이미 결정된 값을 바로 돌려줍니다.
   - 권한이 없어도 예약 호출은 막지 않습니다. Android 는 나중에 권한을 받으면 이미 예약한 알림이 표시되지만, **iOS 는 권한이 없는 동안 OS 가 예약을 버립니다**(서비스의 대기 목록에는 예약됨으로 남습니다). 그래서 권한을 받으면 필요한 알림을 다시 예약해야 합니다.
   - 예약 함수를 "전체 취소 후 다시 예약"으로 만들면 시작, 설정 켜짐, 권한 허용 때 여러 번 불러도 알림이 겹치지 않습니다.

```csharp
// 필요한 알림을 모두 예약합니다. 먼저 전체 취소하므로 여러 번 불러도 겹치지 않습니다
private void ScheduleAll()
{
    _notifications.CancelAllScheduledNotifications();
    // CreateNotification() 으로 알림을 만들어 ScheduleNotification(...) 으로 예약
}

// OS 권한: 게임 흐름에 맞는 시점(예: 튜토리얼 뒤)에 요청합니다.
// iOS 는 권한이 없던 동안의 예약을 버렸으므로 허용되면 다시 예약합니다
_notifications.RequestPermission(permission =>
{
    if (permission == NotificationPermission.Granted && _notifications.IsEnabled)
    {
        ScheduleAll();
    }
});

// 시작: 저장된 앱 안 설정으로 토글을 맞추고, 켜져 있으면 한 번 예약합니다
toggle.SetIsOnWithoutNotify(_notifications.IsEnabled);
if (_notifications.IsEnabled)
{
    ScheduleAll();
}

// 이후 사용자 조작: 토글을 서비스에 반영하고, 켜질 때 다시 예약합니다(끌 때 모두 지웠으므로)
_notifications.OnEnabledChanged += enabled =>
{
    if (enabled)
    {
        ScheduleAll();
    }
};
toggle.onValueChanged.AddListener(isOn => _notifications.IsEnabled = isOn);

// 설정 화면에 보여 줄 "알림 켜짐" 값
var isOn = _notifications.IsEnabled && _notifications.Permission == NotificationPermission.Granted;
```

### 플랫폼별 차이

| 항목 | Android | iOS |
|------|---------|-----|
| `Channel` | 알림 채널 ID. 지정하지 않으면 첫 번째로 등록한 채널 | `CategoryIdentifier` |
| `Subtitle` | 무시됨 | 지원 |
| `ShouldAutoCancel` | 지원 | 효과 없음 |
| `SmallIcon` / `LargeIcon` | 지원 | 무시됨 |
| `Id` 미지정 시 | 예약할 때 서비스가 자동 ID 를 부여(아래 주의 사항) | 예약할 때 서비스가 자동 ID 를 부여. 내부 식별자가 정수가 아니면 `Id` 는 `null`(경고 로그) |
| `DeliveryTime` | `null` 을 넣으면 `ArgumentNullException` | 캘린더 트리거로 변환되며 밀리초는 무시됨 |
| 포그라운드 전환 | 추가 동작 없음 | 앱 배지를 0 으로 초기화 |
| `RequestPermission` | Android 13 이상이고 targetSdk 33 이상이면 요청 창. 그 밖에는 창 없이 기기 설정 값으로 결정 | 처음 한 번 요청 창, 그 뒤에는 창 없이 결정된 값 |

## 주의 사항

- **에디터에서는 실제로 예약되지 않습니다.** `MobileNotificationService` 는 에디터에서 내부 전용 `EditorGameNotification` 을 반환하고, `ScheduleNotification` 은 ID 만 채운 `PendingNotification` 을 돌려줄 뿐 `PendingNotifications` 에 추가하지 않습니다. 따라서 에디터에서는 전달/만료 이벤트도 발생하지 않습니다. 앱 안 알림 설정은 에디터에서도 지켜지고(꺼져 있으면 `null`), `Permission` 은 항상 `Granted`, `RequestPermission` 은 바로 `Granted` 로 알립니다.
- `MobileNotificationService.ScheduleNotification(null)` 은 `ArgumentNullException` 을 던집니다.
- **지원하지 않는 플랫폼**(Android/iOS 외 빌드)에서는 `CreateNotification()` 이 `null` 을 반환하고 취소/닫기 메서드는 아무 동작도 하지 않습니다.
- **원격(푸시) 알림은 다루지 않습니다.** 수신 처리는 로컬 알림에 대해서만 구현되어 있습니다.
- `Id` 를 지정하지 않으면 예약할 때 1,000,000 부터 1씩 늘어나는 ID 를 부여합니다(모든 모드와 플랫폼, 에디터 포함). 마지막 값은 `PlayerPrefs` 에 저장되어 다음 실행에도 이어지고, 대기 목록에 있는 ID 는 건너뜁니다. 직접 지정하는 ID 는 1,000,000 미만을 쓰세요. 다음 실행에서 같은 알림을 취소하려면 `Id` 를 직접 지정해 두는 것이 안전합니다.
- 백그라운드 전환은 `OnApplicationPause` 로 판단합니다. 결제창이나 광고처럼 앱 위에 창이 떠서 포커스만 잃은 경우에는 큐 예약과 저장을 하지 않습니다. 시작 직후처럼 상태가 바뀌지 않은 호출은 무시합니다.
- `PlayerPrefs` 키 `notifications`(예약 목록), `geuneda.notificationservice.enabled`(앱 안 알림 설정), `geuneda.notificationservice.lastId`(마지막 자동 ID)를 씁니다. 같은 키를 다른 용도로 쓰지 마세요.
- 앱이 백그라운드로 갈 때 예약 상태를 `PlayerPrefs` 키 `notifications` 에 저장하고, 포그라운드로 돌아오면 다시 읽습니다. 앱 안 알림 설정이 꺼져 있으면 포그라운드로 돌아올 때 OS 에 남은 예약을 지우고 저장된 목록을 되살리지 않습니다. 저장되는 값은 `Id`, `Title`, `Body`, `Subtitle`, `Channel`, `BadgeNumber`, `DeliveryTime`, `SmallIcon`, `LargeIcon`, `ShouldAutoCancel` 입니다. 예약을 지우지 않는 모드에서는 OS 예약이 남아 있으므로 복원한 알림을 다시 예약하지 않고 예약됨(`Scheduled`) 상태로 되살립니다. 0.1.7 이하가 저장한 값은 읽을 수 없어 빈 상태로 처리됩니다.
- `AndroidGameNotification` 은 `UNITY_ANDROID`, `iOSGameNotification` 은 `UNITY_IOS` 에서만 컴파일됩니다. 플랫폼 타입을 직접 참조하는 코드는 같은 전처리 지시문으로 감싸야 합니다.

## 요구 사항과 의존성

- Unity 2021.3 이상 (`package.json` 의 `unity` 필드)
- `com.geuneda.core` 0.4.0(로거 `ModuleLogger`)
- `com.unity.mobile.notifications` 2.4.2 이상 (`package.json` 에 선언된 버전. 권한 API 가 이 버전대에 있습니다)
- 어셈블리: `Geuneda.NotificationService` (네임스페이스 `Geuneda.NotificationService`)

변경 이력은 [CHANGELOG.md](CHANGELOG.md) 를 참고하세요.

## 라이선스

MIT. [LICENSE.md](LICENSE.md) 를 참고하세요. 원저작자는 Miguel Tomás(GameLovers)입니다.

# Changelog
이 패키지의 모든 주요 변경사항은 이 파일에 기록됩니다.

이 형식은 [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)를 기반으로 하며,
이 프로젝트는 [Semantic Versioning](http://semver.org/spec/v2.0.0.html)을 따릅니다.

## [0.2.0] - 2026-10-13

**New**:
- 앱 안 알림 설정 `INotificationService.IsEnabled`(기본값 true, `PlayerPrefs`에 저장)와 `OnEnabledChanged` 이벤트를 추가. 끄면 예약된 알림을 모두 취소하고 표시된 알림을 닫으며 저장해 둔 예약 목록도 지우고, 이후 `ScheduleNotification`은 예약하지 않고 null을 반환합니다. 켜도 지운 알림을 되살리지 않으므로 이벤트에서 다시 예약합니다
- OS 알림 권한 API `INotificationService.Permission`, `RequestPermission(Action<NotificationPermission>)`과 `NotificationPermission` 열거형을 추가. Android 13 이상은 `POST_NOTIFICATIONS`, iOS는 알림·배지·소리 권한을 요청합니다. 요청에 응답하기 전에는 `Pending`이고, 에디터에서는 항상 `Granted`입니다. iOS에서 요청 시점을 정하려면 Mobile Notifications 설정의 `Request Authorization on App Launch`를 끕니다
- PlayMode 테스트 추가(`Geuneda.NotificationService.Tests.PlayMode`). 관리자 로직은 내부 `InitializeWithPlatform`으로 가짜 플랫폼을 넣어 검증합니다

**Changed**:
- 의존성 `com.unity.mobile.notifications`를 1.3.0에서 2.4.2로, 최소 Unity를 2019.4에서 2021.3으로 올림(권한 API가 2.x에 있음)
- 백그라운드 판단을 `OnApplicationFocus`에서 `OnApplicationPause`로 바꿈. 결제창이나 광고처럼 포커스만 잃은 경우 큐 예약, 저장, 포그라운드 재처리를 하지 않고, 시작 직후처럼 상태가 바뀌지 않은 호출은 무시합니다
- `Id`가 없는 알림에 1,000,000부터 1씩 늘어나는 ID를 부여(모든 모드와 플랫폼, 에디터 포함). 마지막 값은 OS에 예약하기 전에 `PlayerPrefs`에 저장(`Save`)되어 다음 실행에도 이어지고, 대기 목록에 있는 ID는 건너뜁니다. 이전에는 큐 모드와 에디터가 현재 시각 문자열의 해시를, 큐가 아닌 Android는 OS 발급 ID를, iOS는 Mobile Notifications가 만든 식별자(역시 시각 해시)를 썼습니다. 이를 위해 새 `iOSGameNotification`은 식별자 없이 만들어 `Id`가 null입니다
- Android에서 채널 등록이 실패하면 만든 플랫폼을 정리한 뒤 예외를 다시 던지고, 다시 `Initialize`할 수 있습니다(이전에는 초기화 상태로 남아 재시도할 수 없었습니다)
- `INotificationService`에 멤버가 추가되었으므로 이 인터페이스를 직접 구현한 클래스는 새 멤버를 구현해야 합니다

**Fixed**:
- 같은 순간에 ID 없이 여러 알림을 큐에 넣으면 시각 해시 ID가 겹쳐 서로 덮어쓸 수 있던 문제를 단조 증가 ID로 해결

## [0.1.9] - 2026-10-12

**New**:
- *MobileNotificationService*에 `AutoBadging` 프로퍼티를 추가해 배지 자동 부여 여부를 바꿀 수 있게 함(기본값 true)

## [0.1.8] - 2026-10-12

**New**:
- *MobileNotificationService*에 `OperatingMode`를 지정하는 생성자 `MobileNotificationService(OperatingMode mode, params GameNotificationChannel[] channels)`를 추가

**Fixed**:
- *MobileNotificationService*의 `OnLocalNotificationDeliveredEvent`/`OnLocalNotificationExpiredEvent`를 생성 이후에 구독하면 호출되지 않던 문제를 수정
- *MobileNotificationService.ScheduleNotification*에 null을 넘기면 `NullReferenceException`(에디터) 또는 null 반환(기기) 대신 `ArgumentNullException`을 던지도록 수정
- `PlayerPrefs` 키 `notifications`에 알림이 저장/복원되지 않던 문제를 수정(`JsonUtility`가 직렬화할 수 있는 형식으로 저장). `SmallIcon`, `LargeIcon`, `ShouldAutoCancel`도 저장하고, 백그라운드 전환 시 `PlayerPrefs.Save()`로 즉시 기록. 이전 버전이 저장한 값은 빈 상태로 읽음
- 예약을 지우지 않는 모드에서 복원한 알림이 예약됨 상태가 아니어서 다음 백그라운드 전환 때 저장되지 않던 문제를 수정(OS 예약은 그대로 두고 상태만 복원)
- 전달/만료 이벤트 구독자가 콜백 안에서 취소 API를 호출하면 대기 목록 인덱스가 어긋나거나 범위 예외가 나던 문제를 수정
- 잘못된 코드 주석과 `package.json` 설명의 "원격 알림 수신"을 정정

## [0.1.7] - 2020-09-24

**Fixed**:
- 도달 불가 코드 컴파일러 경고를 수정

## [0.1.6] - 2020-08-03

**Fixed**:
- 에디터에서 알림 예약 시 발생하는 NullReferenceException 오류를 수정

## [0.1.5] - 2020-08-03

**Fixed**:
- 에디터에서 *IGameNotification* 처리 시 발생하는 NullReferenceException 오류를 수정

## [0.1.4] - 2020-08-03

**Fixed**:
- Android 빌드 오류를 수정

## [0.1.3] - 2020-08-03

**Fixed**:
- iOS에서 저장된 알림 직렬화를 수정

## [0.1.2] - 2020-07-29

**Fixed**:
- Android 알림 채널을 수정

## [0.1.1] - 2020-07-29

**Fixed**:
- 누락된 gitignore 및 gitattributes 파일을 추가
- Android 알림을 수정

## [0.1.0] - 2020-07-29

- 패키지 배포를 위한 최초 제출

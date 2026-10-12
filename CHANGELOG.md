# Changelog
이 패키지의 모든 주요 변경사항은 이 파일에 기록됩니다.

이 형식은 [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)를 기반으로 하며,
이 프로젝트는 [Semantic Versioning](http://semver.org/spec/v2.0.0.html)을 따릅니다.

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

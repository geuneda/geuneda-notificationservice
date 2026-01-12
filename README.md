# Geuneda Notification Service

모바일 기기에 전송되는 알림을 관리하기 위한 Unity 서비스 패키지입니다.

## 개요

Unity의 Mobile Notifications 패키지를 기반으로 하여 모바일 알림을 쉽게 관리할 수 있게 해주는 패키지입니다.

## 주요 기능

- 로컬 알림 예약
- 원격 알림 수신
- 모든 유형의 알림 삭제

## 요구 사항

- Unity 2019.4 이상
- Unity Mobile Notifications 패키지 (`com.unity.mobile.notifications`)

## 설치 방법

### Unity Package Manager를 통한 설치

1. Unity 에디터에서 `Window` > `Package Manager`를 엽니다.
2. 좌측 상단의 `+` 버튼을 클릭하고 `Add package from git URL...`을 선택합니다.
3. 다음 URL을 입력합니다:
   ```
   https://github.com/geuneda/geuneda-notificationservice.git
   ```
4. `Add` 버튼을 클릭합니다.

### manifest.json을 통한 설치

프로젝트의 `Packages/manifest.json` 파일에 다음을 추가합니다:

```json
{
  "dependencies": {
    "com.geuneda.notificationservice": "https://github.com/geuneda/geuneda-notificationservice.git"
  }
}
```

## 네임스페이스

```csharp
using Geuneda.NotificationService;
```

## 라이선스

MIT License

# ExhibitionClient (Unreal Engine 5)

UE5 기반 전시 3D 씬 클라이언트.  
WebSocket으로 로컬 서버와 연결하고, 수신한 명령으로 캐릭터 애니메이션·감정·HUD를 제어합니다.

> **Note**: `Content/` 폴더(에셋)는 라이센스 문제로 제외되어 있습니다.  
> C++ 소스(`Source/`)와 설정(`Config/`)만 포함합니다.  
> Blueprint 로직은 [Docs/BP_ThirdPersonCharacter.md](Docs/BP_ThirdPersonCharacter.md)에 텍스트로 정리했습니다.

---

## C++ 모듈 구조

```
Source/ExhibitionClient/
├── Public/
│   ├── Realtime/       # WebSocket 연결 및 메시지 처리
│   ├── Messaging/      # 수신 JSON → 명령 구조체 파싱
│   ├── Input/          # 패널 입력 → UE 입력 변환
│   ├── UI/             # HUD 위젯 및 매니저
│   ├── Launch/         # 서버 EXE 자동 실행
│   └── Spawn/          # 캐릭터 스폰 관리
└── Private/            # 각 헤더의 구현부
```

---

## 클래스 설명

### Realtime/

| 클래스 | 파일 | 역할 | 왜 이 구조인가 |
|--------|------|------|---------------|
| `ExhibitionRealtimeSubsystem` | [`Public/Realtime/ExhibitionRealtimeSubsystem.h`](Source/ExhibitionClient/Public/Realtime/ExhibitionRealtimeSubsystem.h) | `GameInstanceSubsystem`. WebSocket 연결 관리, 수신 메시지를 Delegate로 브로드캐스트. | 씬 전환과 무관하게 WebSocket 연결이 유지되어야 해서 GameInstanceSubsystem으로 구현. |
| `ExhibitionWsClient` | [`Public/Realtime/ExhibitionWsClient.h`](Source/ExhibitionClient/Public/Realtime/ExhibitionWsClient.h) | `IWebSocket` 래퍼. 연결·재연결·메시지 송수신. | WebSocket 로직을 Subsystem에서 분리해 독립적으로 교체 가능. |
| `ExhibitionRealtimeTypes.h` | [`Public/Realtime/ExhibitionRealtimeTypes.h`](Source/ExhibitionClient/Public/Realtime/ExhibitionRealtimeTypes.h) | `OnMoveDirection`, `OnRotate`, `OnChatReply` 등 Delegate 타입 선언. | 모든 이벤트를 한 파일에서 관리해 의존 관계 파악이 쉬움. |

### Messaging/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `ExhibitionCommandParser` | [`Public/Messaging/ExhibitionCommandParser.h`](Source/ExhibitionClient/Public/Messaging/ExhibitionCommandParser.h) | 수신 JSON → `FExhibitionCommand` 구조체 변환. |
| `ExhibitionCommandDtos.h` | [`Public/Messaging/ExhibitionCommandDtos.h`](Source/ExhibitionClient/Public/Messaging/ExhibitionCommandDtos.h) | `setEmotion`, `playAnimation`, `triggerStageEvent` DTO 구조체 정의. |
| `ExhibitionCommandType.h` | [`Public/Messaging/ExhibitionCommandType.h`](Source/ExhibitionClient/Public/Messaging/ExhibitionCommandType.h) | 명령 타입 열거형. |

### Input/

| 클래스 | 파일 | 역할 | 왜 이 구조인가 |
|--------|------|------|---------------|
| `ExhibitionPawnInputBridge` | [`Public/Input/ExhibitionPawnInputBridge.h`](Source/ExhibitionClient/Public/Input/ExhibitionPawnInputBridge.h) / [`Private/Input/ExhibitionPawnInputBridge.cpp`](Source/ExhibitionClient/Private/Input/ExhibitionPawnInputBridge.cpp) | `ActorComponent`. `OnMoveDirection`·`OnRotate` Delegate 구독 → Enhanced Input 변환. | Character 클래스 수정 없이 탈부착 가능. 캐릭터 ID 필터링으로 멀티 캐릭터 확장 용이. |
| `ExhibitionQuitHandler` | [`Public/Input/ExhibitionQuitHandler.h`](Source/ExhibitionClient/Public/Input/ExhibitionQuitHandler.h) / [`Private/Input/ExhibitionQuitHandler.cpp`](Source/ExhibitionClient/Private/Input/ExhibitionQuitHandler.cpp) | `ActorComponent`. ESC 키 → `QuitGame()`. | 관람객은 패널 조작, 관리자만 키보드 접근 → ESC로 종료. |

**이동 vs 회전 처리 방식 차이** ([`ExhibitionPawnInputBridge.cpp`](Source/ExhibitionClient/Private/Input/ExhibitionPawnInputBridge.cpp) 참고):

```
이동 (Joystick):
  패널이 80ms마다 절댓값(-1~1) 전송 → PendingMoveInput 저장
  → Tick마다 InjectInputForAction 재주입 (1프레임만 유효하므로)
  → 타임아웃(0.25s) 초과 시 자동 정지 (패널 앱 백그라운드 전환 대비)

회전 (TouchPad):
  패널이 이벤트마다 델타값 전송 → AddYawInput / SetControlRotation 즉시 1회
  → Pitch: NormalizeAxis 후 ±MaxPitchAngle 클램프 (정수리/바닥 끝 방지)
```

### UI/

| 클래스 | 파일 | 역할 | 왜 이 구조인가 |
|--------|------|------|---------------|
| `ExhibitionHudManager` | [`Public/UI/ExhibitionHudManager.h`](Source/ExhibitionClient/Public/UI/ExhibitionHudManager.h) / [`Private/UI/ExhibitionHudManager.cpp`](Source/ExhibitionClient/Private/UI/ExhibitionHudManager.cpp) | `GameInstanceSubsystem`. HUD 생성·QR 제어·채팅 자동 숨김 타이머. | 생명주기 관리와 표시 로직을 Widget과 분리. |
| `ExhibitionHudWidget` | [`Public/UI/ExhibitionHudWidget.h`](Source/ExhibitionClient/Public/UI/ExhibitionHudWidget.h) / [`Private/UI/ExhibitionHudWidget.cpp`](Source/ExhibitionClient/Private/UI/ExhibitionHudWidget.cpp) | `UUserWidget`. `BindWidgetOptional`로 UMG 바인딩. SetText·SetVisibility만 노출. | 표시 전용 — UMG 디자이너 작업과 코드 작업 분리. |

**채팅 답변 표시 흐름:**

```
WebSocket "chatReply" 수신
    → ExhibitionRealtimeSubsystem.OnChatReply Delegate
    → ExhibitionHudManager.OnChatReply()
        ├─ HudWidget.ShowChatReply(text)
        └─ SetTimer(8초) → HudWidget.HideChatReply()
```

### Launch/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `ExhibitionServerLauncher` | [`Public/Launch/ExhibitionServerLauncher.h`](Source/ExhibitionClient/Public/Launch/ExhibitionServerLauncher.h) / [`Private/Launch/ExhibitionServerLauncher.cpp`](Source/ExhibitionClient/Private/Launch/ExhibitionServerLauncher.cpp) | `GameInstanceSubsystem`. 게임 시작 시 서버 EXE 자동 실행, 게임 종료 시 함께 종료. |

서버 EXE 경로 (`Config/Windows/WindowsGame.ini`):
```ini
[/Script/ExhibitionClient.ExhibitionServerLauncher]
ServerExePath=ExhibitionServer/ExhibitionServer.exe
```

---

## 설정 파일

| 파일 | 주요 설정 |
|------|-----------|
| [`Config/DefaultGame.ini`](Config/DefaultGame.ini) | HUD 위젯 클래스, WebSocket URL, 패널 포트 |
| [`Config/Windows/WindowsGame.ini`](Config/Windows/WindowsGame.ini) | 패키지 빌드용 서버 EXE 경로 |
| [`Config/DefaultEngine.ini`](Config/DefaultEngine.ini) | 플러그인, 렌더링 설정 |
| [`Config/DefaultInput.ini`](Config/DefaultInput.ini) | Enhanced Input 기본 매핑 |

---

## 기술 선택 이유

| 결정 | 이유 |
|------|------|
| `GameInstanceSubsystem` (Realtime·HUD·Launcher) | 씬 전환과 무관하게 생명주기 유지 필요. Actor·Component보다 관리가 단순. |
| `ActorComponent` (InputBridge·QuitHandler) | 캐릭터에 탈부착 가능. 캐릭터 클래스를 건드리지 않음. |
| `InjectInputForAction` (이동) vs `AddYawInput` (회전) | 이동은 절댓값 지속 주입, 회전은 델타 1회 적용 — 입력 특성이 달라 별도 처리. |

# ExhibitionClient (Unreal Engine 5)

UE5 기반 전시 3D 씬 클라이언트.  
WebSocket으로 로컬 서버와 연결하고, 서버로부터 수신한 명령으로 캐릭터 애니메이션·감정·HUD를 제어합니다.

> **Note**: `Content/` 폴더(에셋)는 라이센스 문제로 제외되어 있습니다.  
> C++ 소스(`Source/`)와 설정(`Config/`)만 포함합니다.

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

| 클래스 | 역할 | 왜 이 구조인가 |
|--------|------|---------------|
| `ExhibitionRealtimeSubsystem` | `GameInstanceSubsystem`. WebSocket 연결 관리 + 수신 메시지를 Delegate로 브로드캐스트. | GameInstance 생명주기와 동일하게 씬 전환 시에도 연결이 유지되어야 하므로 Subsystem으로 구현. |
| `ExhibitionWsClient` | `IWebSocket` 래퍼. 연결·재연결·메시지 송수신 처리. | WebSocket 로직을 Subsystem에서 분리해 테스트·교체 용이. |
| `ExhibitionRealtimeTypes.h` | Delegate 타입 선언 (`OnMoveDirection`, `OnRotate`, `OnChatReply` 등). | 모든 이벤트를 한 파일에서 관리해 의존 관계 파악이 쉬움. |

### Messaging/

| 클래스 | 역할 |
|--------|------|
| `ExhibitionCommandParser` | 수신된 JSON 문자열을 파싱해 `FExhibitionCommand` 구조체로 변환. |
| `ExhibitionCommandDtos.h` | `setEmotion`, `playAnimation`, `triggerStageEvent` 등 명령 DTO 구조체 정의. |
| `ExhibitionCommandType.h` | 명령 타입 열거형. |

### Input/

| 클래스 | 역할 | 왜 이 구조인가 |
|--------|------|---------------|
| `ExhibitionPawnInputBridge` | `ActorComponent`. Subsystem의 `OnMoveDirection`·`OnRotate` Delegate를 구독해 Enhanced Input으로 변환. | Character 클래스 수정 없이 탈부착 가능. 2캐릭터 확장 시 ID 필터링만 변경하면 됨. |
| `ExhibitionQuitHandler` | `ActorComponent`. ESC 키 입력 시 `QuitGame()` 호출. | 관리자만 키보드 접근 가능한 전시 환경에서 운영자 종료용. |

**이동 vs 회전 처리 방식 차이**

```
이동 (Joystick):
  패널 → 80ms마다 절댓값(-1~1) 전송
  → PendingMoveInput 저장
  → Tick마다 InjectInputForAction (1프레임 유효이므로 매 프레임 재주입)

회전 (TouchPad):
  패널 → 이벤트마다 델타값 전송
  → AddYawInput / SetControlRotation 즉시 1회 호출 (Tick 불필요)
  → Pitch는 NormalizeAxis 후 ±MaxPitchAngle 클램프
```

### UI/

| 클래스 | 역할 | 왜 이 구조인가 |
|--------|------|---------------|
| `ExhibitionHudManager` | `GameInstanceSubsystem`. HUD 위젯 생성·제어, QR 표시 로직, 채팅 자동 숨김 타이머 관리. | HUD 생명주기 관리를 Widget과 분리해 Widget은 순수 표시만 담당. |
| `ExhibitionHudWidget` | `UUserWidget`. `BindWidgetOptional`로 UMG 요소 바인딩. SetText, SetVisibility 등 단순 표시 메서드만 노출. | 비즈니스 로직 없이 표시 전용으로 유지해 UMG 디자이너 작업과 코드 작업을 분리. |

**채팅 답변 표시 흐름:**

```
서버 WebSocket → "chatReply" JSON 수신
    │
    ▼
ExhibitionRealtimeSubsystem.OnChatReply Delegate
    │
    ▼
ExhibitionHudManager.OnChatReply()
    ├─ HudWidget.ShowChatReply(text)    ← 텍스트 표시
    └─ SetTimer(8초) → HudWidget.HideChatReply()  ← 자동 숨김
```

### Launch/

| 클래스 | 역할 |
|--------|------|
| `ExhibitionServerLauncher` | `GameInstanceSubsystem`. 게임 시작 시 `FPlatformProcess::CreateProc`로 서버 EXE 자동 실행. 게임 종료 시 서버 프로세스도 함께 종료. |

서버 EXE 경로는 `Config/Windows/WindowsGame.ini`에서 설정:
```ini
[/Script/ExhibitionClient.ExhibitionServerLauncher]
ServerExePath=ExhibitionServer/ExhibitionServer.exe
```

---

## Blueprint 로직 문서

Blueprint 파일(`.uasset`)은 바이너리라 이 리포지터리에 포함되지 않습니다.  
핵심 Blueprint 로직을 아래 문서에 텍스트로 정리했습니다.

- [BP_ThirdPersonCharacter 감정·애니메이션 처리](Docs/BP_ThirdPersonCharacter.md)

---

## 설정 파일

| 파일 | 주요 설정 |
|------|-----------|
| `Config/DefaultGame.ini` | HUD 위젯 클래스, WebSocket URL, 패널 포트 |
| `Config/Windows/WindowsGame.ini` | 패키지 빌드용 서버 EXE 경로 오버라이드 |
| `Config/DefaultEngine.ini` | 플러그인, 렌더링 설정 |
| `Config/DefaultInput.ini` | Enhanced Input 기본 매핑 |

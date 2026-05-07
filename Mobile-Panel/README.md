# Mobile-Panel

관람객이 스마트폰/태블릿 브라우저에서 접속하는 **React PWA 모바일 컨트롤러**.  
서버에서 정적 파일로 서빙되며, SignalR로 서버와 실시간 통신합니다.

---

## 폴더 구조

```
src/
├── core/
│   ├── hooks/          # 입력 처리 커스텀 훅
│   ├── input/          # 조이스틱·터치패드·버튼 UI 컴포넌트
│   ├── layout/         # 컨트롤러 공통 레이아웃
│   └── transport/      # 서버 통신 계층 (SignalR / HTTP)
└── panels/
    └── exhibition/     # 전시 전용 패널 진입점 + 채팅 API
```

---

## 클래스 / 모듈 설명

### `core/hooks/`

| 파일 | 역할 | 사용처 |
|------|------|--------|
| `useJoystick.ts` | 터치 조이스틱의 벡터 값(-1~1) 계산. `pointerId`로 멀티터치 분리. | `Joystick.tsx` |
| `useTouchPad.ts` | 터치패드 드래그 델타 값 계산 (카메라 회전용). | `TouchPad.tsx` |
| `useThrottle.ts` | 전송 주기 제어 (80ms). 불필요한 SignalR 메시지 방지. | `ExhibitionPanel.tsx` |

### `core/input/`

| 파일 | 역할 |
|------|------|
| `Joystick.tsx` | 가상 조이스틱 UI. 터치 시작점 기준 상대 벡터 계산. |
| `TouchPad.tsx` | 카메라 회전용 터치 영역. 드래그 델타를 pitch/yaw로 변환. |
| `ActionButton.tsx` | 단발성 액션 버튼 (애니메이션, 감정 트리거 등). |

### `core/layout/`

| 파일 | 역할 |
|------|------|
| `ControllerShell.tsx` | 전체 컨트롤러 레이아웃 컨테이너. 세로/가로 방향 대응. |
| `StatusBar.tsx` | 서버 연결 상태 표시 (연결됨 / 연결 중 / 끊김). |
| `SideMenu.tsx` | 채팅 입력창 포함 사이드 패널. |

### `core/transport/`

| 파일 | 역할 | 왜 이 구조인가 |
|------|------|---------------|
| `SignalRTransport.ts` | `@microsoft/signalr` 기반 실시간 통신. `moveDirection`, `rotate`, `command` 메시지 전송. | UE WebSocket과 SignalR이 별도로 존재하므로 Transport를 분리해 교체 가능하게 설계. |
| `HttpTransport.ts` | 채팅 등 요청-응답 구조의 REST 호출. | 스트리밍 응답(SSE)도 이 계층에서 처리. |
| `TransportProvider.tsx` | Context로 Transport 인스턴스를 하위 컴포넌트에 주입. | 패널 교체 시 Transport 재생성 없이 재사용. |
| `types.ts` | Transport 인터페이스 정의. | 추후 WebSocket 직접 연결 등 다른 구현체로 교체 가능. |

### `panels/exhibition/`

| 파일 | 역할 |
|------|------|
| `ExhibitionPanel.tsx` | 전시 패널 진입점. 조이스틱·터치패드·채팅을 조합하고 80ms 주기로 서버에 입력 전송. |
| `chatApi.ts` | AI 채팅 HTTP 요청 및 SSE 스트리밍 수신 처리. |
| `commands.ts` | 버튼 클릭 → 서버로 전송할 명령 타입 정의 (setEmotion, playAnimation 등). |

---

## 데이터 흐름

```
사용자 터치
    │
    ├─ Joystick → useJoystick → FVector2D(-1~1)
    │                          → useThrottle(80ms)
    │                          → SignalRTransport.sendMoveDirection()
    │
    ├─ TouchPad → useTouchPad → 드래그 델타(pitch, yaw)
    │                         → SignalRTransport.sendRotate()
    │
    └─ 채팅 입력 → chatApi.ts → POST /api/chat
                              → SSE 스트리밍 수신 → UI 표시
```

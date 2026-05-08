# Mobile-Panel

관람객이 스마트폰/태블릿 브라우저에서 접속하는 **React PWA 모바일 컨트롤러**.  
서버에서 정적 파일로 서빙되며, SignalR로 서버와 실시간 통신합니다.  
음성 입력(STT) → AI 응답(LLM) → 음성 출력(TTS) 파이프라인을 포함합니다.

---

## 실행

```bash
npm install
npm run dev      # http://0.0.0.0:3001 (개발)
npm run build    # dist/ 생성 (배포용)
```

환경변수: [`.env.example`](.env.example) 참고

---

## 폴더 구조

```
src/
├── core/
│   ├── audio/          # TTS 큐 플레이어 (AudioContext 기반)
│   ├── hooks/          # 입력·음성 녹음 커스텀 훅
│   ├── input/          # 조이스틱·터치패드·버튼·마이크 UI 컴포넌트
│   ├── layout/         # 컨트롤러 공통 레이아웃
│   └── transport/      # 서버 통신 계층 (SignalR / HTTP)
└── panels/
    └── exhibition/     # 전시 전용 패널 진입점 + 채팅·음성 API
```

---

## 모듈 설명

### `core/audio/`

| 파일 | 역할 |
|------|------|
| [`TtsQueuePlayer.ts`](src/core/audio/TtsQueuePlayer.ts) | TTS 오디오 URL을 순서대로 재생하는 큐 플레이어. `enqueue()` 시점에 즉시 fetch+decode(프리패치)하여 청크 간 무음 갭 제거. SSE 스트림 종료(`finalize()`) 후에만 `onComplete` 발동. |

### `core/hooks/`

| 파일 | 역할 | 사용처 |
|------|------|--------|
| [`useJoystick.ts`](src/core/hooks/useJoystick.ts) | 터치 조이스틱의 벡터 값(-1~1) 계산. `pointerId`로 멀티터치 분리. | `Joystick.tsx` |
| [`useTouchPad.ts`](src/core/hooks/useTouchPad.ts) | 터치패드 드래그 델타 값 계산 (카메라 회전용). | `TouchPad.tsx` |
| [`useThrottle.ts`](src/core/hooks/useThrottle.ts) | 전송 주기 제어 (80ms). 불필요한 SignalR 메시지 방지. | `ExhibitionPanel.tsx` |
| [`useVoiceRecorder.ts`](src/core/hooks/useVoiceRecorder.ts) | MediaRecorder 기반 음성 녹음 훅. 런타임 MIME 자동 감지(webm→mp4→wav). `start()` / `stop()→Blob` 인터페이스. | `ExhibitionPanel.tsx` |

### `core/input/`

| 파일 | 역할 |
|------|------|
| [`Joystick.tsx`](src/core/input/Joystick.tsx) | 가상 조이스틱 UI. 터치 시작점 기준 상대 벡터 계산. |
| [`TouchPad.tsx`](src/core/input/TouchPad.tsx) | 카메라 회전용 터치 영역. 드래그 델타를 pitch/yaw로 변환. |
| [`ActionButton.tsx`](src/core/input/ActionButton.tsx) | 단발성 액션 버튼 (애니메이션, 감정 트리거 등). |
| [`MicButton.tsx`](src/core/input/MicButton.tsx) | Push-to-Talk 마이크 버튼. `pointerdown`/`pointerup`/`pointerleave`로 터치·마우스 모두 대응. 녹음 중 pulse 애니메이션, 권한 거부 상태 표시. |

### `core/layout/`

| 파일 | 역할 |
|------|------|
| [`ControllerShell.tsx`](src/core/layout/ControllerShell.tsx) | 전체 컨트롤러 레이아웃 컨테이너. 세로/가로 방향 대응. |
| [`StatusBar.tsx`](src/core/layout/StatusBar.tsx) | 서버 연결 상태 표시 (연결됨 / 연결 중 / 끊김). |
| [`SideMenu.tsx`](src/core/layout/SideMenu.tsx) | 채팅 입력창 포함 사이드 패널. |

### `core/transport/`

Transport 계층을 인터페이스로 분리한 이유: SignalR(실시간)과 HTTP(채팅 SSE)가 역할이 달라 교체·확장이 필요하고, 패널 종류가 늘어도 Transport를 재사용할 수 있게 설계.

| 파일 | 역할 |
|------|------|
| [`SignalRTransport.ts`](src/core/transport/SignalRTransport.ts) | `@microsoft/signalr` 기반 실시간 통신. `moveDirection`, `rotate`, `command` 전송. |
| [`HttpTransport.ts`](src/core/transport/HttpTransport.ts) | 채팅 REST 호출 및 SSE 스트리밍 수신. |
| [`TransportProvider.tsx`](src/core/transport/TransportProvider.tsx) | Context로 Transport 인스턴스를 하위 컴포넌트에 주입. |
| [`types.ts`](src/core/transport/types.ts) | Transport 인터페이스 정의. |

### `panels/exhibition/`

| 파일 | 역할 |
|------|------|
| [`ExhibitionPanel.tsx`](src/panels/exhibition/ExhibitionPanel.tsx) | 전시 패널 진입점. 조이스틱·터치패드·채팅·음성을 조합하고 80ms 주기로 입력 전송. |
| [`chatApi.ts`](src/panels/exhibition/chatApi.ts) | AI 채팅 HTTP 요청 및 SSE 스트리밍 수신 처리. |
| [`voiceApi.ts`](src/panels/exhibition/voiceApi.ts) | 음성 채팅 SSE 파이프라인. `fetch + ReadableStream`으로 `ack` / `transcript` / `text_chunk` / `tts_chunk` / `done` / `error` 이벤트 파싱. |
| [`commands.ts`](src/panels/exhibition/commands.ts) | 버튼 → 서버 명령 타입 정의 (setEmotion, playAnimation 등). |

---

## 데이터 흐름

```
사용자 터치
    │
    ├─ Joystick → useJoystick → 벡터(-1~1)
    │                         → useThrottle(80ms)
    │                         → SignalRTransport.sendMoveDirection()
    │
    ├─ TouchPad → useTouchPad → 드래그 델타(pitch, yaw)
    │                         → SignalRTransport.sendRotate()
    │
    ├─ 채팅 입력 → chatApi.ts → POST /api/chat
    │                        → SSE 스트리밍 수신 → UI 표시
    │
    └─ 마이크 버튼 (Push-to-Talk)
           │  누름 → useVoiceRecorder.start()
           │  뗌   → useVoiceRecorder.stop() → Blob
           │
           └─ voiceApi.streamVoiceChat()
                  │  POST /api/voice/chat (multipart)
                  │
                  ├─ onAck       → TtsQueuePlayer.enqueue(ackUrl)
                  ├─ onTranscript → 채팅창에 사용자 메시지 표시
                  ├─ onTextChunk  → 채팅창 스트리밍 텍스트 업데이트
                  ├─ onTtsChunk  → TtsQueuePlayer.enqueue(ttsUrl)
                  ├─ onDone      → TtsQueuePlayer.finalize()
                  └─ onError     → 에러 메시지 표시

           TtsQueuePlayer: 재생 완료 → notifySpeakingComplete()
                                     → POST /api/voice/speaking-complete
```

---

## 기술 선택 이유

| 결정 | 이유 |
|------|------|
| SignalR (웹소켓 대신) | 자동 재연결·폴백 내장, 서버와 같은 ASP.NET Core 스택에서 허브 관리가 단순 |
| 80ms throttle | UE 입력 주입이 1프레임 유효 → 60fps 기준 16ms마다 갱신되므로 80ms로도 충분히 부드러움 |
| PWA | 앱 설치 없이 브라우저만으로 전시 운영 가능 |

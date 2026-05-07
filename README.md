# Interactive Exhibition

관람객이 **모바일 패널(React PWA)**로 **UE5 캐릭터**를 실시간 조종하고, AI 챗봇과 대화하며 전시물을 탐색하는 인터랙티브 전시 시스템.

---

## 시스템 아키텍처

```
┌─────────────────────────────────────────────────────────────────┐
│  관람객 (폰/태블릿)                                              │
│  Mobile Panel (React PWA)                                        │
│    조이스틱 조종 / 카메라 회전 / AI 채팅                         │
└────────────────────┬────────────────────────────────────────────┘
                     │ SignalR (WebSocket)
┌────────────────────▼────────────────────────────────────────────┐
│  로컬 서버 (전시 PC)                                             │
│  ASP.NET Core — ExhibitionServer                                 │
│    SignalR Hub  │  WebSocket(UE용)  │  채팅 RAG 파이프라인       │
│    키워드 검색(전시물 JSON) → 후보 컨텍스트 선별                  │
└──────┬─────────────────────────────┬──────────────────────────── ┘
       │ WebSocket                   │ HTTPS (RAG + LLM 위임)
┌──────▼──────────────┐   ┌──────────▼──────────────────────────── ┐
│  UE5 클라이언트     │   │  Azure — ExhibitionAiGateway            │
│  ExhibitionClient   │   │    Embedding Rerank (OpenAI)            │
│    캐릭터 애니메이션 │   │    LLM 호출 (OpenAI gpt-4o-mini)        │
│    감정 표현        │   │    API 키는 이곳에만 존재               │
│    HUD 채팅 표시    │   └─────────────────────────────────────────┘
└─────────────────────┘
```

---

## 컴포넌트별 문서

| 컴포넌트 | 경로 | 문서 |
|----------|------|------|
| 모바일 패널 (React PWA) | `Mobile-Panel/` | [README](Mobile-Panel/README.md) |
| 로컬 서버 (ASP.NET SignalR) | `Server-AspNet/ExhibitionServer/` | [README](Server-AspNet/ExhibitionServer/README.md) |
| AI Gateway (Azure) | `Cloud-AI-Gateway/ExhibitionAiGateway/` | [README](Cloud-AI-Gateway/ExhibitionAiGateway/README.md) |
| UE5 클라이언트 (C++) | `Client-Unreal/ExhibitionClient/` | [README](Client-Unreal/ExhibitionClient/README.md) |
| 공유 DTO | `Shared/Exhibition.Shared/` | — |

---

## 기능별 핵심 코드 바로가기

### 실시간 통신

| 기능 | 파일 |
|------|------|
| SignalR Hub (패널 ↔ 서버) | [ExhibitionHub.cs](Server-AspNet/ExhibitionServer/Realtime/ExhibitionHub.cs) |
| UE WebSocket 미들웨어 | [UnrealWebSocketMiddleware.cs](Server-AspNet/ExhibitionServer/Realtime/UnrealWebSocketMiddleware.cs) |
| UE 연결 관리자 | [UnrealConnectionManager.cs](Server-AspNet/ExhibitionServer/Realtime/UnrealConnectionManager.cs) |
| UE 리얼타임 서브시스템 | [ExhibitionRealtimeSubsystem.h](Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Public/Realtime/ExhibitionRealtimeSubsystem.h) |
| WebSocket 클라이언트 (UE) | [ExhibitionWsClient.h](Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Public/Realtime/ExhibitionWsClient.h) |

### AI 채팅 파이프라인 (RAG)

| 기능 | 파일 |
|------|------|
| 채팅 오케스트레이션 (RAG → Gateway → 명령 파싱) | [ChatGuideService.cs](Server-AspNet/ExhibitionServer/Application/Chat/ChatGuideService.cs) |
| AI Gateway HTTP 클라이언트 | [AiGatewayClient.cs](Server-AspNet/ExhibitionServer/Application/Chat/AiGatewayClient.cs) |
| 전시물 키워드 검색 | [ExhibitionKnowledgeStore.cs](Server-AspNet/ExhibitionServer/Application/Knowledge/ExhibitionKnowledgeStore.cs) |
| Embedding Rerank (Gateway측) | [EmbeddingRetrievedContextRanker.cs](Cloud-AI-Gateway/ExhibitionAiGateway/Application/Rag/EmbeddingRetrievedContextRanker.cs) |
| OpenAI 프롬프트 + 스트리밍 | [OpenAiResponsesProvider.cs](Cloud-AI-Gateway/ExhibitionAiGateway/Providers/OpenAI/OpenAiResponsesProvider.cs) |
| 대화 히스토리 관리 | [ConversationMemoryStore.cs](Server-AspNet/ExhibitionServer/Application/Chat/ConversationMemoryStore.cs) |

### 모바일 패널 입력

| 기능 | 파일 |
|------|------|
| 조이스틱 훅 | [useJoystick.ts](Mobile-Panel/src/core/hooks/useJoystick.ts) |
| 터치패드 훅 (카메라 회전) | [useTouchPad.ts](Mobile-Panel/src/core/hooks/useTouchPad.ts) |
| SignalR 트랜스포트 | [SignalRTransport.ts](Mobile-Panel/src/core/transport/SignalRTransport.ts) |
| 전시 패널 진입점 | [ExhibitionPanel.tsx](Mobile-Panel/src/panels/exhibition/ExhibitionPanel.tsx) |

### UE5 입력 처리

| 기능 | 파일 |
|------|------|
| 패널 입력 → UE Enhanced Input 변환 | [ExhibitionPawnInputBridge.cpp](Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/Input/ExhibitionPawnInputBridge.cpp) |
| 관리자 종료 키 핸들러 | [ExhibitionQuitHandler.cpp](Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/Input/ExhibitionQuitHandler.cpp) |

### UE5 UI / HUD

| 기능 | 파일 |
|------|------|
| HUD 매니저 (QR, 연결 수, 채팅 표시) | [ExhibitionHudManager.cpp](Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/UI/ExhibitionHudManager.cpp) |
| HUD 위젯 (UMG 바인딩) | [ExhibitionHudWidget.cpp](Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/UI/ExhibitionHudWidget.cpp) |

### 서버 자동 실행 / 배포

| 기능 | 파일 |
|------|------|
| 서버 EXE 자동 실행 (GameInstanceSubsystem) | [ExhibitionServerLauncher.cpp](Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/Launch/ExhibitionServerLauncher.cpp) |
| 패널 빌드 → wwwroot 복사 스크립트 | [copy-panel.sh](scripts/copy-panel.sh) |
| 서버 publish 스크립트 | [publish-server.bat](scripts/publish-server.bat) |

---

## 기술 스택

| 영역 | 기술 |
|------|------|
| 3D 클라이언트 | Unreal Engine 5, C++, Blueprint |
| 서버 | ASP.NET Core, SignalR, WebSocket |
| AI Gateway | ASP.NET Core, OpenAI API, Azure App Service |
| 모바일 패널 | React, TypeScript, Vite, Tailwind CSS |
| 공유 타입 | .NET 8 Class Library |
| 배포 | itch.io (UE 패키지), Azure (AI Gateway) |

---

## 실행 방법

### 개발 환경

```bash
# 1. 모바일 패널
cd Mobile-Panel && npm install && npm run dev
# http://0.0.0.0:3001

# 2. 로컬 서버
cd Server-AspNet/ExhibitionServer && dotnet run
# http://localhost:5225

# 3. AI Gateway (Azure 또는 로컬)
cd Cloud-AI-Gateway/ExhibitionAiGateway && dotnet run
# 환경변수 필요: OpenAI__ApiKey, Gateway__ApiKey

# 4. UE5 에디터에서 실행
```

### 배포 빌드

```bash
# 패널 빌드 → 서버 wwwroot 동기화 → 서버 publish
cd Mobile-Panel && npm run build && cd ..
bash scripts/copy-panel.sh
scripts\publish-server.bat
# UE Editor → Platforms → Windows → Package Project
```

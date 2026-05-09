# ExhibitionServer (ASP.NET Core)

전시 PC에서 실행되는 **로컬 서버**.  
모바일 패널(SignalR) ↔ UE5 클라이언트(WebSocket) 간 중계 역할을 하며,  
AI 채팅 RAG 파이프라인과 음성 채팅(STT→LLM→TTS) 파이프라인을 오케스트레이션합니다.

---

## 실행

```bash
dotnet run   # http://0.0.0.0:5225
```

**HTTPS 자동 활성화**: 실행 폴더에 `server.pfx`가 존재하면 [`appsettings.LocalHttps.json`](appsettings.LocalHttps.json)을 자동 로드해 HTTPS `7225` 포트도 함께 엽니다.  
`server.pfx`가 없으면 HTTP만 동작합니다 (itch.io 공개 배포 기본값).

설정: [`appsettings.json`](appsettings.json) — `AiGateway.BaseUrl` 을 Gateway 주소로 변경  
배포: [`../../scripts/publish-server.bat`](../../scripts/publish-server.bat)

---

## 폴더 구조

```
ExhibitionServer/
├── Realtime/               # SignalR Hub, UE WebSocket 미들웨어·연결 관리
├── Application/
│   ├── Chat/               # 채팅 RAG 파이프라인 (핵심)
│   ├── Voice/              # 음성 채팅 파이프라인 (STT→LLM→TTS)
│   ├── Knowledge/          # 전시물 지식베이스 (키워드 검색)
│   └── Abstractions/       # 인터페이스 정의
├── Controllers/            # REST API 엔드포인트
├── Configuration/          # DI 등록, 미들웨어 파이프라인, Kestrel 튜닝
│                           #   KestrelConfiguration: server.pfx 감지 + 저지연 옵션
├── Options/                # appsettings 바인딩 클래스
├── Streaming/              # SSE 스트리밍 유틸
├── appsettings.json        # 기본 설정 (HTTP 5225)
├── appsettings.LocalHttps.json  # HTTPS 7225 설정 (server.pfx 존재 시 자동 로드)
└── Data/ExhibitionKnowledge/ # 전시물 JSON 데이터
```

---

## 클래스 설명

### Realtime/

| 클래스 | 역할 | 왜 이 구조인가 |
|--------|------|---------------|
| [`ExhibitionHub.cs`](Realtime/ExhibitionHub.cs) | SignalR Hub. 패널 연결/해제 수신, UE로 명령 브로드캐스트. | Hub는 얇게 유지 — 비즈니스 로직은 Application 계층으로 위임. |
| [`UnrealWebSocketMiddleware.cs`](Realtime/UnrealWebSocketMiddleware.cs) | UE5 전용 WebSocket 연결 수락 및 메시지 루프. | SignalR과 UE WebSocket이 다른 프로토콜이므로 별도 미들웨어로 분리. |
| [`UnrealConnectionManager.cs`](Realtime/UnrealConnectionManager.cs) | UE WebSocket 연결 관리, 메시지 직렬화·전송. | 다중 UE 연결 확장 시 이 클래스만 수정하면 됨. |
| [`IRawUnrealBroadcaster.cs`](Realtime/Abstractions/IRawUnrealBroadcaster.cs) | JSON 문자열을 UE로 전송하는 최소 인터페이스. | ChatGuideService가 Realtime 구현체에 직접 의존하지 않도록 의존성 역전. |

### Application/Chat/ — RAG 파이프라인

```
패널 채팅 메시지
    │
    ▼
ChatGuideService.HandleAsync()
    │
    ├─ 1. ExhibitionKnowledgeStore  → 키워드 검색으로 관련 전시물 후보 선별
    ├─ 2. ConversationMemoryStore   → 최근 N턴 대화 히스토리 조회
    ├─ 3. AiGatewayClient           → Gateway에 {메시지 + 컨텍스트 + 히스토리} 전송
    │       └─ Gateway: Embedding Rerank + LLM 호출 → {reply + suggestedCommands}
    ├─ 4. CompleteCommands()        → 응답 명령 파싱·보완 (누락된 setEmotion 자동 추가)
    ├─ 5. CommandDispatcher         → UE로 setEmotion, playAnimation 등 전송
    └─ 6. BroadcastChatReplyAsync() → UE HUD에 채팅 텍스트 표시
```

| 클래스 | 파일 |
|--------|------|
| `ChatGuideService` | [`Application/Chat/ChatGuideService.cs`](Application/Chat/ChatGuideService.cs) |
| `AiGatewayClient` | [`Application/Chat/AiGatewayClient.cs`](Application/Chat/AiGatewayClient.cs) |
| `ConversationMemoryStore` | [`Application/Chat/ConversationMemoryStore.cs`](Application/Chat/ConversationMemoryStore.cs) |
| `ConversationLogger` | [`Application/Chat/ConversationLogger.cs`](Application/Chat/ConversationLogger.cs) |

**왜 OpenAI 키를 이 서버에 두지 않는가**  
배포 시 서버 EXE가 사용자 PC에 설치됩니다. API 키가 포함되면 노출 위험이 있어  
키는 Azure AI Gateway에만 보관하고, 로컬 서버는 Gateway URL로만 통신합니다.  
→ [`DisabledEmbeddingClient.cs`](Application/Knowledge/DisabledEmbeddingClient.cs)

### Application/Knowledge/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `ExhibitionKnowledgeStore` | [`Application/Knowledge/ExhibitionKnowledgeStore.cs`](Application/Knowledge/ExhibitionKnowledgeStore.cs) | 전시물 JSON을 시작 시 메모리에 로드. 키워드·태그·별칭 검색. |
| `InMemoryKnowledgeVectorSearch` | [`Application/Knowledge/InMemoryKnowledgeVectorSearch.cs`](Application/Knowledge/InMemoryKnowledgeVectorSearch.cs) | 벡터 검색 확장을 위한 인메모리 스텁. |
| `DisabledEmbeddingClient` | [`Application/Knowledge/DisabledEmbeddingClient.cs`](Application/Knowledge/DisabledEmbeddingClient.cs) | Embedding 비활성화 구현체. 로컬 서버에서 항상 사용. |

### Application/Voice/ — 음성 채팅 파이프라인

```
패널 음성 입력 (Blob)
    │  POST /api/voice/chat
    ▼
VoiceChatService.StreamAsync()   ← IAsyncEnumerable<VoiceStreamEvent> SSE
    │
    ├─ 1. AiGatewayVoiceClient.TranscribeAsync()  → Gateway STT (Whisper)
    ├─ 2. FastAckService.Select()                 → 캐시 wav 즉시 반환 (latency masking)
    ├─ 3. ChatGuideService.StreamAsync()           → RAG + LLM 스트리밍
    │       └─ SentenceBuffer                     → 문장 완성 시 TTS 호출
    │            └─ AiGatewayVoiceClient.TtsAsync() → Gateway TTS
    │                 └─ TtsAudioStore.Store()    → /api/voice/audio/{id} URL 발급
    └─ 4. SendPlayAnimationAsync()                → UE에 explain 애니메이션 제어
         (loop:true=TTS시작 / loop:false=재생완료)
```

**저지연 기법:**
- **Fast Ack**: STT 완료 즉시 캐시 오디오 재생 — LLM 대기 시간 차폐
- **문장 단위 TTS 스트리밍**: 전체 응답 완성 전에 첫 문장 TTS 재생 시작
- **VoiceMode 플래그**: `ChatRequest.VoiceMode=true`이면 ChatGuideService가 `PlayAnimationCommand` dispatch 스킵 (애니메이션 타이밍을 VoiceChatService가 독점 제어)

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `VoiceChatService` | [`Application/Voice/VoiceChatService.cs`](Application/Voice/VoiceChatService.cs) | 파이프라인 오케스트레이터. |
| `SentenceBuffer` | [`Application/Voice/SentenceBuffer.cs`](Application/Voice/SentenceBuffer.cs) | LLM delta 누적 → 문장 경계 감지. |
| `TtsAudioStore` | [`Application/Voice/TtsAudioStore.cs`](Application/Voice/TtsAudioStore.cs) | TTS 오디오 인메모리 임시 저장. TTL 10분. |
| `FastAckService` | [`Application/Voice/FastAckService.cs`](Application/Voice/FastAckService.cs) | transcript 분류 → 캐시 wav 즉시 반환. |
| `VoiceStreamEvent` | [`Application/Voice/VoiceStreamEvent.cs`](Application/Voice/VoiceStreamEvent.cs) | SSE 이벤트 타입 정의 (ack/transcript/text_chunk/tts_chunk/done/error). |
| `AiGatewayVoiceClient` | [`Application/Chat/AiGatewayVoiceClient.cs`](Application/Chat/AiGatewayVoiceClient.cs) | Gateway STT/TTS HTTP 클라이언트. |

**Fast Ack 캐시 파일 생성:**
```bash
cd Data/TtsCache
node generate_ack.js   # OPENAI_API_KEY 환경변수 필요
```

### Controllers/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `ChatController` | [`Controllers/ChatController.cs`](Controllers/ChatController.cs) | `POST /api/chat` — 채팅 수신, SSE 스트리밍 응답. |
| `VoiceChatController` | [`Controllers/VoiceChatController.cs`](Controllers/VoiceChatController.cs) | `POST /api/voice/chat` — 음성 수신, SSE 스트리밍. `GET /api/voice/audio/{id}` — TTS wav 제공. `POST /api/voice/speaking-complete` — 재생 완료 알림. |
| `CommandsController` | [`Controllers/CommandsController.cs`](Controllers/CommandsController.cs) | `POST /api/commands` — 직접 명령 전송 (디버그용). |
| `PanelAccessController` | [`Controllers/PanelAccessController.cs`](Controllers/PanelAccessController.cs) | 패널 접속 URL·QR 정보 제공. |

---

## 미들웨어 파이프라인

[`Configuration/MiddlewareExtensions.cs`](Configuration/MiddlewareExtensions.cs) 참고:

```
1. Swagger (개발 환경)
2. UseDefaultFiles + UseStaticFiles  ← 모바일 패널 정적 서빙 (wwwroot/)
3. UseCors
4. UseAuthorization
5. UseWebSockets
6. UseUnrealWebSocket               ← UE WebSocket 연결
7. MapControllers + MapHub          ← REST API + SignalR Hub
```

---

## 전시물 데이터

[`Data/ExhibitionKnowledge/`](Data/ExhibitionKnowledge/) — 전시물 1개 = JSON 파일 1개.  
`dotnet publish` 시 자동으로 출력 폴더에 포함됩니다 (`.csproj`에 `CopyToPublishDirectory` 설정).

```json
{
  "id": "artifact_triceratops",
  "title": "트리케라톱스",
  "zone": "고생물 전시관",
  "summary": "...",
  "tags": ["공룡", "백악기", "초식"],
  "aliases": ["Triceratops", "뿔공룡"]
}
```

---

## 기술 선택 이유

| 결정 | 이유 |
|------|------|
| SignalR (패널용) + 별도 WebSocket (UE용) | UE는 SignalR 클라이언트 라이브러리가 없음. 패널은 자동 재연결이 필요해 SignalR이 적합. |
| 키워드 검색 로컬 + Embedding Rerank Gateway | API 키 없이도 후보 선별 가능. 정밀 rerank는 키 있는 Gateway에서만 수행. |
| `GameInstanceSubsystem` 패턴 (UE측) | 씬 전환과 무관하게 WebSocket 연결 유지 필요. |

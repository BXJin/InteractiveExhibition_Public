# ExhibitionServer (ASP.NET Core)

전시 PC에서 실행되는 **로컬 서버**.  
모바일 패널(SignalR) ↔ UE5 클라이언트(WebSocket) 간 중계 역할을 하며,  
AI 채팅 RAG 파이프라인을 오케스트레이션합니다.

---

## 폴더 구조

```
ExhibitionServer/
├── Realtime/               # SignalR Hub, UE WebSocket 미들웨어·연결 관리
├── Application/
│   ├── Chat/               # 채팅 RAG 파이프라인 (핵심)
│   ├── Knowledge/          # 전시물 지식베이스 (키워드 검색)
│   └── Abstractions/       # 인터페이스 정의
├── Controllers/            # REST API 엔드포인트
├── Configuration/          # DI 등록, 미들웨어 파이프라인, Kestrel 설정
├── Options/                # appsettings 바인딩 클래스
├── Streaming/              # SSE 스트리밍 유틸
└── Data/ExhibitionKnowledge/ # 전시물 JSON 데이터
```

---

## 클래스 설명

### Realtime/

| 클래스 | 역할 | 왜 이 구조인가 |
|--------|------|---------------|
| `ExhibitionHub` | SignalR Hub. 패널 연결/해제 이벤트 수신, UE로 명령 브로드캐스트. | SignalR 그룹 관리와 UE 전달을 명확히 분리하기 위해 Hub를 얇게 유지. |
| `UnrealWebSocketMiddleware` | UE5 클라이언트의 WebSocket 연결 수락 및 메시지 루프 처리. | SignalR과 UE WebSocket이 다른 프로토콜이므로 별도 미들웨어로 분리. |
| `UnrealConnectionManager` | UE WebSocket 연결 1개를 유지하고 메시지 직렬화/전송 담당. | 다중 UE 연결 확장 시 이 클래스만 수정하면 됨. |
| `IRawUnrealBroadcaster` | 문자열 JSON을 UE로 전송하는 최소 인터페이스. | ChatGuideService가 Realtime 구현체에 직접 의존하지 않도록 역전. |

### Application/Chat/ — RAG 파이프라인 핵심

```
패널 채팅 메시지
    │
    ▼
ChatGuideService.HandleAsync()
    │
    ├─ 1. ExhibitionKnowledgeStore  → 키워드 검색으로 관련 전시물 후보 선별
    │
    ├─ 2. ConversationMemoryStore   → 최근 N턴 대화 히스토리 조회
    │
    ├─ 3. AiGatewayClient           → AI Gateway에 RAG 요청 (후보 컨텍스트 포함)
    │       └─ Gateway가 Embedding Rerank + LLM 호출 후 응답 반환
    │
    ├─ 4. CompleteCommands()        → 응답에서 감정·애니메이션 명령 파싱·보완
    │
    ├─ 5. CommandDispatcher         → UE로 setEmotion, playAnimation 등 명령 전송
    │
    └─ 6. BroadcastChatReplyAsync() → UE HUD에 채팅 텍스트 표시 (chatReply 메시지)
```

| 클래스 | 역할 |
|--------|------|
| `ChatGuideService` | 위 흐름 전체를 오케스트레이션. |
| `AiGatewayClient` | AI Gateway HTTP 호출. 스트리밍(SSE)과 단건 응답 모두 지원. |
| `ConversationMemoryStore` | 세션별 대화 히스토리를 메모리에 유지 (최근 N턴). |
| `ConversationLogger` | 대화 내용 로그 기록. |

**왜 OpenAI 키를 이 서버에 두지 않는가**  
Steam 배포 시 서버 EXE가 사용자 PC에 설치됩니다. API 키가 포함되면 노출 위험이 있어,  
키는 Azure의 AI Gateway에만 보관하고, 로컬 서버는 키 없이 Gateway URL로만 통신합니다.

### Application/Knowledge/

| 클래스 | 역할 |
|--------|------|
| `ExhibitionKnowledgeStore` | 전시물 JSON을 시작 시 메모리에 로드. 키워드·태그·별칭 기반 검색. |
| `InMemoryKnowledgeVectorSearch` | 추후 벡터 검색 확장을 위한 인메모리 스텁. |
| `DisabledEmbeddingClient` | Embedding 기능 비활성화 구현체. 로컬 서버는 키가 없으므로 항상 이것을 사용. |

### Controllers/

| 클래스 | 역할 |
|--------|------|
| `ChatController` | `POST /api/chat` — 채팅 요청 수신, SSE 스트리밍 응답 반환. |
| `CommandsController` | `POST /api/commands` — 직접 명령 전송 (테스트/디버그용). |
| `PanelAccessController` | 패널 접속 URL, QR 코드 관련 정보 제공. |

---

## 미들웨어 파이프라인 순서

`Configuration/MiddlewareExtensions.cs` 참고:

```
1. Swagger (개발 환경)
2. UseDefaultFiles + UseStaticFiles  ← 모바일 패널 정적 서빙
3. UseCors
4. UseAuthorization
5. UseWebSockets
6. UseUnrealWebSocket               ← UE WebSocket 연결 처리
7. MapControllers + MapHub          ← REST API + SignalR
```

---

## 전시물 데이터

`Data/ExhibitionKnowledge/*.json` — 전시물 1개 = JSON 파일 1개.  
서버 시작 시 전부 로드되며, `dotnet publish` 시 자동으로 출력 폴더에 포함됩니다.

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

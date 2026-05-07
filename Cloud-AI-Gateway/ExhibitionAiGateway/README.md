# ExhibitionAiGateway (ASP.NET Core — Azure)

**Azure App Service에 배포되는 AI 프록시 서버**.  
로컬 ExhibitionServer로부터 RAG 요청을 받아 Embedding Rerank 후 LLM을 호출하고 응답을 반환합니다.  
OpenAI API 키는 이 서버의 환경변수에만 존재합니다.

---

## 왜 별도 서버인가

| 이유 | 설명 |
|------|------|
| **API 키 보안** | 로컬 서버는 Steam으로 사용자 PC에 배포됨. 키 포함 불가. |
| **Embedding 연산 분리** | Embedding Rerank는 OpenAI 호출이 필요 → 키 있는 Gateway에서만 가능. |
| **LLM 교체 유연성** | Provider 패턴으로 OpenAI / Gemini / Groq / LocalLLM 교체 가능. |

---

## 폴더 구조

```
ExhibitionAiGateway/
├── Controllers/            # HTTP 엔드포인트
├── Application/
│   ├── Abstractions/       # IAiChatService, IAiChatProvider, IRetrievedContextRanker 등
│   ├── Rag/                # Embedding Rerank
│   └── AiChatService.cs    # 단건 응답 오케스트레이션
│   └── AiChatStreamService.cs # 스트리밍 응답 오케스트레이션
├── Providers/
│   └── OpenAI/             # OpenAI Responses API 구현체
├── Security/               # API 키 인증 미들웨어
├── Options/                # appsettings 바인딩
└── Streaming/              # SSE 유틸
```

---

## 클래스 설명

### 요청 흐름

```
ExhibitionServer (로컬)
    │  POST /ai/chat  {message, retrievedContext, conversationHistory}
    ▼
AiGatewayAuthenticationMiddleware   ← X-AI-Gateway-Key 헤더 검증
    │
    ▼
AiChatController
    │
    ├─ 스트리밍 요청 → AiChatStreamService
    │       │
    │       ├─ EmbeddingRetrievedContextRanker  ← 후보 컨텍스트 cosine similarity 재정렬
    │       └─ OpenAiResponsesProvider          ← OpenAI Responses API (SSE 스트리밍)
    │               └─ ReplyStreamExtractor     ← delta 청크 누적 → 명령 포함 JSON 추출
    │
    └─ 단건 요청 → AiChatService → OpenAiResponsesProvider (비스트리밍)
```

### Application/

| 클래스 | 역할 |
|--------|------|
| `AiChatService` | 단건 채팅 응답 오케스트레이션 (Rerank → LLM → 응답 반환). |
| `AiChatStreamService` | SSE 스트리밍 응답 오케스트레이션. |
| `AiChatProviderResolver` | `appsettings`의 `AiProvider.Provider` 값으로 구현체 선택 (OpenAI / Gemini / Groq / Local). |

### Application/Rag/

| 클래스 | 역할 | 왜 이 구조인가 |
|--------|------|---------------|
| `EmbeddingRetrievedContextRanker` | 로컬 서버가 보낸 후보 컨텍스트를 질의와 cosine similarity로 재정렬. 상위 N개만 LLM에 전달. | 로컬 서버는 키워드 검색으로 후보를 넓게 잡고, Gateway가 의미 기반으로 정밀하게 좁힘. |
| `OpenAiEmbeddingClient` | `text-embedding-3-small`로 텍스트 → 벡터 변환. 결과를 인메모리 캐시에 보관. | 동일 컨텍스트 반복 임베딩 방지 (전시물 수가 적어 캐시가 효과적). |

### Providers/OpenAI/

| 클래스 | 역할 |
|--------|------|
| `OpenAiResponsesProvider` | OpenAI Responses API 호출. 프롬프트 구성, 스트리밍 파싱, JSON 응답 역직렬화. |
| `ReplyStreamExtractor` | SSE delta 청크를 누적해 완성된 JSON 블록(`reply` + `suggestedCommands`)을 추출. |

### Security/

| 클래스 | 역할 |
|--------|------|
| `AiGatewayAuthenticationMiddleware` | `X-AI-Gateway-Key` 헤더가 없거나 불일치하면 401 반환. 외부 직접 접근 차단. |

---

## 환경변수 (Azure App Service에만 설정)

```
OpenAI__ApiKey          = sk-...
Gateway__ApiKey         = <게이트웨이 접근 키>
OpenAI__Model           = gpt-4o-mini
OpenAI__EmbeddingModel  = text-embedding-3-small
```

로컬 `appsettings.json`에 API 키를 넣지 마세요.

---

## LLM 응답 형식

Gateway는 LLM에게 아래 JSON 스키마를 강제합니다:

```json
{
  "reply": "한국어 안내 응답",
  "suggestedCommands": [
    { "type": "setEmotion", "characterId": "Character_01", "emotion": "explaining" },
    { "type": "playAnimation", "characterId": "Character_01", "animation": "explain" }
  ]
}
```

`OpenAiResponsesProvider.BuildInstructions()`에서 프롬프트로 강제하며,  
파싱 실패 시 raw 텍스트를 `reply`로 fallback 처리합니다.

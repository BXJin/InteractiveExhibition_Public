# ExhibitionAiGateway (ASP.NET Core — Azure)

**Azure App Service에 배포되는 AI 프록시 서버**.  
로컬 ExhibitionServer로부터 RAG 요청을 받아 Embedding Rerank 후 LLM을 호출하고 응답을 반환합니다.  
음성 채팅 파이프라인을 위한 STT(Whisper) / TTS(gpt-4o-mini-tts) 엔드포인트도 제공합니다.  
OpenAI API 키는 이 서버의 환경변수에만 존재합니다.

---

## 실행

```bash
dotnet run   # http://localhost:5100
```

필수 환경변수:
```
OpenAI__ApiKey       = sk-...
GatewaySecurity__ApiKey = <접근 제한 키>
```

---

## 왜 별도 서버인가

| 이유 | 설명 |
|------|------|
| **API 키 보안** | 로컬 서버는 사용자 PC에 배포됨. 키 포함 불가. |
| **Embedding 연산 분리** | Embedding Rerank는 OpenAI 호출이 필요 → 키 있는 Gateway에서만 가능. |
| **LLM 교체 유연성** | Provider 패턴으로 OpenAI / Gemini / Groq / LocalLLM 설정만으로 교체 가능. |

---

## 폴더 구조

```
ExhibitionAiGateway/
├── Controllers/            # HTTP 엔드포인트
│   ├── AiChatController.cs
│   └── AiVoiceController.cs
├── Application/
│   ├── Abstractions/       # 인터페이스 정의 (IAiChatProvider, ISttProvider, ITtsProvider 등)
│   ├── Rag/                # Embedding Rerank
│   ├── AiChatService.cs    # 단건 응답 오케스트레이션
│   └── AiChatStreamService.cs # 스트리밍 응답 오케스트레이션
├── Providers/OpenAI/       # OpenAI 구현체
│   ├── OpenAiResponsesProvider.cs  # Chat (Responses API)
│   ├── OpenAiSttProvider.cs        # STT (Whisper)
│   ├── OpenAiTtsProvider.cs        # TTS (gpt-4o-mini-tts)
│   └── ReplyStreamExtractor.cs
├── Security/               # API 키 인증 미들웨어
├── Options/                # appsettings 바인딩 (VoiceOptions 포함)
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
    ├─ 스트리밍 → AiChatStreamService
    │     ├─ EmbeddingRetrievedContextRanker  ← cosine similarity 재정렬
    │     └─ OpenAiResponsesProvider          ← OpenAI SSE 스트리밍
    │             └─ ReplyStreamExtractor     ← delta → JSON 추출
    │
    └─ 단건   → AiChatService → OpenAiResponsesProvider

ExhibitionServer (로컬)
    │  POST /ai/voice/transcribe  (multipart audio)
    │  POST /ai/voice/synthesize  {text, voice, instructions}
    ▼
AiGatewayAuthenticationMiddleware
    │
    ▼
AiVoiceController
    ├─ /transcribe → OpenAiSttProvider  ← Whisper API → transcript 텍스트
    └─ /synthesize → OpenAiTtsProvider  ← gpt-4o-mini-tts → WAV 바이트 스트림
```

### Application/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `AiChatService` | [`Application/AiChatService.cs`](Application/AiChatService.cs) | 단건 요청: Rerank → LLM → 응답 반환. |
| `AiChatStreamService` | [`Application/AiChatStreamService.cs`](Application/AiChatStreamService.cs) | SSE 스트리밍 오케스트레이션. |
| `AiChatProviderResolver` | [`Application/AiChatProviderResolver.cs`](Application/AiChatProviderResolver.cs) | appsettings 값으로 LLM 구현체 선택. |

### Application/Rag/

| 클래스 | 파일 | 역할 | 왜 이 구조인가 |
|--------|------|------|---------------|
| `EmbeddingRetrievedContextRanker` | [`Application/Rag/EmbeddingRetrievedContextRanker.cs`](Application/Rag/EmbeddingRetrievedContextRanker.cs) | 후보 컨텍스트를 쿼리와 cosine similarity로 재정렬. 상위 N개만 LLM에 전달. | 로컬 서버가 키워드로 넓게 잡은 후보를 의미 기반으로 좁혀 LLM 토큰 낭비 방지. |
| `OpenAiEmbeddingClient` | [`Application/Rag/OpenAiEmbeddingClient.cs`](Application/Rag/OpenAiEmbeddingClient.cs) | `text-embedding-3-small`로 텍스트 → 벡터 변환. 인메모리 캐시 적용. | 전시물 수가 적어 인메모리 캐시만으로 반복 임베딩 비용 제거 가능. |

### Providers/OpenAI/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `OpenAiResponsesProvider` | [`Providers/OpenAI/OpenAiResponsesProvider.cs`](Providers/OpenAI/OpenAiResponsesProvider.cs) | OpenAI Responses API 호출. 프롬프트 구성, 스트리밍 파싱, JSON 역직렬화. |
| `ReplyStreamExtractor` | [`Providers/OpenAI/ReplyStreamExtractor.cs`](Providers/OpenAI/ReplyStreamExtractor.cs) | SSE delta 청크를 누적해 `reply + suggestedCommands` JSON 추출. |
| `OpenAiSttProvider` | [`Providers/OpenAI/OpenAiSttProvider.cs`](Providers/OpenAI/OpenAiSttProvider.cs) | Whisper API 호출. multipart/form-data로 오디오 업로드 → 텍스트 반환. |
| `OpenAiTtsProvider` | [`Providers/OpenAI/OpenAiTtsProvider.cs`](Providers/OpenAI/OpenAiTtsProvider.cs) | gpt-4o-mini-tts 호출. 텍스트 → WAV 스트림 반환. `VoiceOptions`에서 목소리·지시문 설정. |

### Controllers/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `AiChatController` | [`Controllers/AiChatController.cs`](Controllers/AiChatController.cs) | `POST /ai/chat` — 채팅 수신, SSE 스트리밍 응답. |
| `AiVoiceController` | [`Controllers/AiVoiceController.cs`](Controllers/AiVoiceController.cs) | `POST /ai/voice/transcribe` — STT(Whisper). `POST /ai/voice/synthesize` — TTS(gpt-4o-mini-tts). |

### Security/

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `AiGatewayAuthenticationMiddleware` | [`Security/AiGatewayAuthenticationMiddleware.cs`](Security/AiGatewayAuthenticationMiddleware.cs) | `X-AI-Gateway-Key` 헤더 검증. 불일치 시 401 반환. |

---

## LLM 응답 스키마

[`Providers/OpenAI/OpenAiResponsesProvider.cs`](Providers/OpenAI/OpenAiResponsesProvider.cs)의 `BuildInstructions()`에서 아래 형식을 강제합니다:

```json
{
  "reply": "한국어 안내 응답",
  "suggestedCommands": [
    { "type": "setEmotion", "characterId": "Character_01", "emotion": "curious" },
    { "type": "playAnimation", "characterId": "Character_01", "animation": "explain" }
  ]
}
```

**감정 선택 기준**: 사용자 질문의 키워드가 아닌 AI 답변의 톤에 따라 결정됩니다.  
인사 → `greeting`, 사실 질문 → `curious`, 역사/분석 → `thinking`, 놀라운 사실 → `surprise`, 구조적 소개 → `explaining` 등.  
`setEmotion`은 응답마다 반드시 1개 포함하도록 프롬프트에 명시되어 있습니다.

**파싱 전략 (3단계 fallback)**:
1. `JsonSerializer.Deserialize` — 정상 파싱
2. `JsonDocument`로 `reply` 필드만 추출 — `suggestedCommands` 파싱 실패 시
3. 빈 `reply` 반환 — SSE 스트리밍 delta가 이미 텍스트를 전달했으므로 덮어쓰지 않음

---

## VoiceOptions 설정 (`appsettings.json`)

```json
"Voice": {
  "SttModel": "whisper-1",
  "TtsModel": "gpt-4o-mini-tts",
  "DefaultVoice": "alloy",
  "TtsInstructions": "한국어로 또렷하고 명확하게 발음해. 단어를 늘이거나 감정을 과장하지 마.",
  "TtsResponseFormat": "wav"
}
```

목소리 변경: `DefaultVoice` 값을 `alloy / echo / fable / onyx / nova / shimmer` 중 하나로 교체.

---

## 기술 선택 이유

| 결정 | 이유 |
|------|------|
| Responses API (Chat Completions 대신) | `output_text` 필드로 텍스트 추출이 단순하고, 스트리밍 이벤트 타입이 명확해 파싱 안정성이 높음. |
| 로컬 키워드 검색 + Gateway Embedding Rerank | 로컬 서버에 키 없이도 동작하면서 의미 기반 정밀도를 확보하는 절충안. |
| Provider 패턴 | 프롬프트와 API 호출을 구현체에 가두고, 설정값만 바꿔 LLM을 교체할 수 있게 설계. |
| STT/TTS를 Gateway에 위임 | API 키 보안 원칙 동일 — 로컬 서버는 키 없이 오디오 바이트만 전달. |

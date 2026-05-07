# AI Chat Streaming Architecture Plan

작성일: 2026-04-28
최종 수정: 2026-04-28

## 0. 결론

AI 전시 가이드 챗봇은 나중에 streaming을 붙이는 것보다, 지금 provider 계약 단계에서 streaming을 고려해 두는 편이 낫다.

endpoint는 역할에 따라 명시적으로 분리한다. 단일 endpoint에서 응답 형식을 동적으로 전환하지 않는다.

```text
기존 유지:
POST /api/chat              → 항상 JSON 응답
POST /api/ai/chat           → 항상 JSON 응답

추가:
POST /api/chat/stream       → 항상 SSE 스트리밍
POST /api/ai/chat/stream    → 항상 SSE 스트리밍
```

기본값은 Stream이다. Mobile Panel은 기본적으로 `/api/chat/stream`을 사용한다.
JSON endpoint는 fallback, 디버깅, 개발 도구 용도로 유지한다.

---

## 1. 왜 지금 고려해야 하는가

현재 Gateway provider 구조는 아직 굳어지는 중이다.

현재 비스트리밍 흐름:

```text
IAiChatProvider.CreateReplyAsync()
-> Task<AiChatResponse>

IAiChatService.CreateReplyAsync()
-> Task<AiChatResponse>

AiChatController.CreateReply()
-> JSON 응답 1회 반환

ExhibitionServer AiGatewayClient.CreateReplyAsync()
-> Gateway 응답 완료까지 대기

ChatGuideService.ProcessAsync()
-> 응답 완료 후 command 검증/실행
```

Gemini, Groq, LocalLlm provider가 아직 실제 구현되기 전이므로, 지금 streaming 계약을 추가하면 이후 provider를 두 번 구현하지 않아도 된다.

늦게 붙이면 생기는 비용:

- provider 인터페이스 재설계
- Gateway controller 추가 변경
- ExhibitionServer proxy endpoint 추가 변경
- Mobile Panel fetch 로직 재작성
- provider별 일반 응답/streaming 응답 중복 구현
- 이미 안정화된 command 검증 경로를 다시 건드릴 위험

---

## 2. Streaming과 command의 충돌

이 프로젝트는 단순 채팅이 아니다.

```text
AI reply
+ RAG context
+ suggestedCommands
+ server whitelist validation
+ Unreal command dispatch
```

텍스트 streaming은 중간 토큰을 바로 보여줄 수 있지만, `suggestedCommands`는 전체 응답을 끝까지 받은 뒤 검증해야 한다. 중간 토큰만 보고 Unreal 명령을 실행하면 안전하지 않다.

따라서 streaming 구조는 2단계로 나눈다.

```text
1단계: reply token streaming → Mobile Panel에 즉시 표시
2단계: complete event에서 suggestedCommands 전달
3단계: ExhibitionServer가 command whitelist 검증
4단계: 검증된 command만 Unreal에 dispatch
```

사용자 체감:

```text
텍스트 답변은 즉시 타이핑되듯 표시
답변이 끝나면 캐릭터 감정/애니메이션/무대 연출 실행
```

---

## 3. 권장 이벤트 프로토콜

Streaming은 SSE(Server-Sent Events)를 사용한다.

이유:

- 브라우저에서 처리하기 쉽다.
- 텍스트 이벤트 스트림에 적합하다.
- WebSocket보다 endpoint 추가가 단순하다.
- 기존 SignalR/Unreal 실시간 제어와 역할이 섞이지 않는다.

이벤트 형식:

```text
event: delta
data: {"text":"안녕하세요"}

event: delta
data: {"text":". 저는 전시 안내"}

event: complete
data: {
  "reply": "안녕하세요. 저는 전시 안내 AI 가이드입니다.",
  "suggestedCommands": [
    { "type": "setEmotion", "characterId": "Character_01", "emotion": "happy" },
    { "type": "playAnimation", "characterId": "Character_01", "animation": "wave" }
  ],
  "provider": "OpenAI",
  "model": "gpt-4o-mini",
  "latencyMs": 1200
}

event: error
data: {"errorCode":"AI_PROVIDER_REQUEST_FAILED","message":"AI provider request failed."}
```

`delta`는 UI 표시 전용이다. `complete.suggestedCommands`만 command 검증 대상으로 삼는다.

---

## 4. Provider 인터페이스 계획

기존 일반 응답 메서드는 유지한다.

```csharp
Task<AiChatResponse> CreateReplyAsync(
    AiChatRequest request,
    CancellationToken cancellationToken);
```

Streaming 메서드를 추가한다.

```csharp
IAsyncEnumerable<AiChatStreamEvent> StreamReplyAsync(
    AiChatRequest request,
    CancellationToken cancellationToken);
```

공통 stream event DTO:

```csharp
public sealed record AiChatStreamEvent
{
    public required string EventType { get; init; } // "delta" | "complete" | "error"
    public string? Text { get; init; }              // delta 전용
    public AiChatResponse? CompleteResponse { get; init; } // complete 전용
    public string? ErrorCode { get; init; }          // error 전용
}
```

Provider별 구현 방향:

| Provider | Streaming 구현 방향 |
|---|---|
| OpenAI | Responses API streaming |
| Gemini | Gemini streaming endpoint |
| Groq | OpenAI-compatible streaming |
| LocalLlm | Ollama `/api/chat` stream 또는 OpenAI-compatible local endpoint |

---

## 5. Endpoint 계획

### Gateway

```http
POST /api/ai/chat         → 항상 JSON  (디버깅, fallback)
POST /api/ai/chat/stream  → 항상 SSE   (운영 기본 경로)
```

### Local ExhibitionServer

```http
POST /api/chat        → 항상 JSON  (디버깅, fallback, rule-based 포함)
POST /api/chat/stream → 항상 SSE   (운영 기본 경로)
```

`/api/chat/stream` 역할:

- Mobile Panel에 SSE stream 전달
- Gateway stream 실패 시 local rule-based fallback을 complete event로 반환
- complete event의 suggestedCommands를 검증 후 Unreal에 dispatch

각 endpoint는 항상 하나의 응답 형식만 반환한다. Content-Type 동적 전환 없음.

---

## 6. StreamMode — 사용자/개발자 선택 옵션

Mobile Panel에서 명시적으로 응답 방식을 선택할 수 있다.

```typescript
type StreamMode = "stream" | "json" | "auto";
```

| 모드 | 동작 | 사용 시점 |
|---|---|---|
| `"stream"` | `/api/chat/stream` 호출 | **기본값**. 운영 시 항상 이 모드 |
| `"json"` | `/api/chat` 호출 | 개발자 디버깅, fallback 테스트 |
| `"auto"` | 클라이언트 분석 후 endpoint 결정 | 실험 기능, 개발자 옵션 |

기본값은 `"stream"`이다.

### Auto 모드 — 클라이언트 side 보조 기능

Auto는 서버가 응답 형식을 바꾸는 것이 아니라, **클라이언트가 어느 endpoint를 호출할지 결정하는 헬퍼**다.

```typescript
function resolveEndpoint(message: string, mode: StreamMode): string {
    if (mode === "json") return "/api/chat";
    if (mode === "stream") return "/api/chat/stream";

    // auto: 메시지 의도 분석 후 endpoint 선택
    return isLikelyShortOrCommand(message)
        ? "/api/chat"
        : "/api/chat/stream";
}
```

Auto 판단 기준 (클라이언트):

| 조건 | 선택 endpoint | 이유 |
|---|---|---|
| 인사 패턴 ("안녕", "반가워") | `/api/chat` | 응답 짧음, stream 불필요 |
| 직접 명령 ("바꿔", "켜줘", "실행") | `/api/chat` | commands 즉시 필요 |
| 20자 이하 짧은 메시지 | `/api/chat` | 짧은 응답 예상 |
| 설명/소개 요청 ("설명해줘", "알려줘") | `/api/chat/stream` | 긴 텍스트 예상 |
| 30자 이상 긴 메시지 | `/api/chat/stream` | 복잡한 응답 예상 |
| 판단 불가 | `/api/chat/stream` | 기본값이 stream |

Auto의 판단 결과와 무관하게 각 endpoint는 항상 동일한 형식을 반환한다. 클라이언트가 어떤 endpoint를 선택했는지 알고 있으므로 응답 처리 방식도 미리 결정된다.

Auto의 위치:

- 포트폴리오에서 "의도 기반 응답 경로 선택" 시연용
- 실험 기능 또는 개발자 옵션으로 노출
- 운영 기본값은 `stream`으로 고정

---

## 7. Mobile Panel 계획

기존 방식:

```ts
const payload = await response.json();
```

Stream 방식 (기본):

```ts
const reader = response.body?.getReader();
// delta 수신 시마다 UI append
// complete 수신 시 metadata 반영
```

UI 상태:

- 사용자가 메시지를 보내면 assistant 메시지 placeholder 생성
- `delta` 수신 시 placeholder text append
- `complete` 수신 시 provider/model/commands 메타데이터 반영
- `error` 수신 시 fallback 메시지 표시

주의:

- stream 중에는 입력 중복 방지
- complete 전에는 command 실행 완료로 표시하지 않음
- 중간 실패 시 partial text 유지 또는 fallback 교체 정책 필요

---

## 8. 구현 순서

### Phase 1. 계약 추가

- `AiChatStreamEvent` DTO 추가 (Shared)
- `IAiChatProvider.StreamReplyAsync` 추가
- `PlaceholderAiChatProvider`는 `NOT_IMPLEMENTED` stream error 반환
- 기존 `CreateReplyAsync`는 유지

### Phase 2. Gateway stream endpoint

- `POST /api/ai/chat/stream` 추가
- SSE response writer 구현
- OpenAI Responses API streaming 구현
- complete event에 `AiChatResponse` 구조 포함

### Phase 3. Local server stream proxy

- `IAiGatewayClient.StreamReplyAsync` 추가
- `POST /api/chat/stream` 추가
- complete event command 검증/dispatch
- Gateway stream 실패 시 local rule-based fallback을 complete event로 반환

### Phase 4. Mobile Panel streaming UI

- 기본 호출 경로를 `/api/chat/stream`으로 변경
- `StreamMode` 선택 옵션 추가 (`stream` / `json` / `auto`)
- `auto` 클라이언트 판단 헬퍼 구현
- assistant placeholder + delta append
- complete metadata 표시

### Phase 5. Provider 확장

- Gemini streaming 구현
- Groq streaming 구현
- LocalLlm streaming 구현

---

## 9. 지금 당장 결정한 것

- streaming을 나중에 완전히 새로 붙이는 것보다 지금 계약을 잡는 편이 낫다.
- endpoint는 역할별로 명시적으로 분리한다. 단일 endpoint에서 Content-Type을 동적으로 바꾸지 않는다.
- 기본값은 `stream`이다. JSON endpoint는 fallback/디버깅 용도로 유지한다.
- `Auto`는 클라이언트 side 헬퍼다. 서버는 항상 요청받은 endpoint 형식으로만 응답한다.
- command 실행은 streaming 중간이 아니라 complete event 이후에만 수행한다.

---

## 10. 포트폴리오 설명 포인트

```text
AI guide response는 SSE streaming으로 사용자에게 즉시 표시하고,
LLM이 제안한 Unreal command는 complete event 이후 서버 측 whitelist 검증을 통과한 경우에만 실행하도록 설계했습니다.

JSON endpoint와 SSE streaming endpoint를 명시적으로 분리해 클라이언트가 응답 형식을 예측 가능하게 유지하면서,
클라이언트 side Auto 판단 헬퍼로 메시지 의도에 따라 적절한 endpoint를 자동 선택하는 옵션도 제공했습니다.

이를 통해 대화 체감 속도, 3D command safety, 응답 경로 예측 가능성을 동시에 확보했습니다.
```

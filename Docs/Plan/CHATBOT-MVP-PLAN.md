# AI 전시 가이드 챗봇 MVP 계획

작성일: 2026-04-26

## 0. 결론

현재 프로젝트의 기본 MVP는 이미 상당 부분 검증됐다.

- Unreal 패키징 실행
- ASP.NET 로컬 서버 동시 실행
- 모바일 패널 노트북 IP 접속
- Happy, Aged, Classic 같은 감정/맵/스타일 명령 실행
- 모바일 패널에서 Unreal 캐릭터와 전시 공간 제어

다음 단계는 단순 채팅창이 아니라, `LLM/RAG 기반 전시 가이드`를 추가하는 것이다.

핵심 목표:

```text
사용자 자연어 질문
-> 전시물 지식 검색
-> LLM 답변 생성
-> 안전한 structured command 생성
-> 서버 검증
-> Unreal 캐릭터/공간 반응
-> 대화 로그 저장
```

## 1. 중요한 방향 전환

처음에는 MVP 안정성을 위해 `RAG`, `Vector DB`, `fine-tuning`, `motion generation`은 바로 넣지 않는 편이 낫다고 봤다. 이유는 구현 리스크가 크고, 패키징/시연 안정성을 해칠 수 있기 때문이다.

하지만 지원 목적을 고려하면 이야기가 달라진다. LLM Engineer 직무에 맞추려면 단순 rule-based 챗봇만으로는 약하다. 그래서 아래처럼 나눈다.

| 구분 | 판단 |
|---|---|
| 시연 안정성만 보면 | rule-based chat + 기존 command 변환이면 충분 |
| LLM Engineer 지원까지 고려하면 | LLM API, prompt engineering, RAG, 로그/데이터셋 구조가 필요 |
| 지금 바로 하기 위험한 것 | 실제 fine-tuning, 실시간 motion generation, 복잡한 Vector DB 운영 |
| 포트폴리오에 넣기 좋은 보강 | in-memory RAG, embedding 선택지, structured output validation, JSONL 로그 |

즉, 이번 챗봇은 “최소 기능”이 아니라 “지원서에서 LLM Engineering을 설명할 수 있는 최소 실무형 구조”로 잡는다.

## 2. 이번 단계에서 구현할 범위

필수 구현:

- 모바일 패널 `Chat` 탭
- ASP.NET `POST /api/chat`
- 전시물 knowledge JSON 3~5개
- 전시물 질문에 대한 답변 생성
- 자연어를 기존 Unreal command로 변환
- LLM structured output
- command whitelist 검증
- 기존 command dispatcher 재사용
- conversation log JSONL 저장
- LLM API 실패 시 rule/keyword fallback

이번 단계에서 제외:

- 실제 fine-tuning
- Qdrant/Chroma 같은 별도 Vector DB 서버
- 실시간 motion generation
- TTS/ASR
- 사용자 계정, 장기 기억, 복잡한 세션 관리

## 3. 사용자 경험

예상 흐름:

```text
1. 사용자가 모바일 패널 Chat 탭을 연다.
2. "이 작품을 밝고 신나는 분위기로 소개해줘"라고 입력한다.
3. 서버가 전시물 context를 검색한다.
4. LLM이 설명 답변과 실행 command를 JSON으로 만든다.
5. 서버가 command를 검증한다.
6. Unreal에 happy 감정, wave/explain 모션, classic 스타일 전환을 보낸다.
7. 패널에는 전시 가이드 답변과 실행 결과가 표시된다.
```

Quick prompt 예시:

- `이 작품을 설명해줘`
- `밝고 신나는 분위기로 소개해줘`
- `차분하고 오래된 전시 분위기로 바꿔줘`

## 4. 서버 구조

제안 구조:

```text
Server-AspNet/ExhibitionServer
  Controllers/
    ChatController.cs
  Application/
    Chat/
      ChatRequest.cs
      ChatResponse.cs
      ChatCommandInterpreter.cs
      ChatPromptBuilder.cs
      ChatResponseParser.cs
      ConversationLogger.cs
    Knowledge/
      ExhibitionKnowledgeDocument.cs
      ExhibitionKnowledgeStore.cs
      InMemoryKnowledgeSearch.cs
    Abstractions/
      IChatCommandInterpreter.cs
      IChatLlmClient.cs
      IExhibitionKnowledgeStore.cs
      IConversationLogger.cs
  Data/
    ExhibitionKnowledge/
      artifact_classic_01.json
      artifact_modern_01.json
      artifact_stage_01.json
    Logs/
      chat-log.jsonl
```

설계 원칙:

- LLM 응답을 바로 실행하지 않는다.
- 서버에서 허용된 command schema로만 변환한다.
- 실패해도 기존 버튼/조이스틱 제어는 유지한다.
- LLM provider는 인터페이스로 분리해 나중에 교체 가능하게 한다.

## 5. API 설계

Endpoint:

```http
POST /api/chat
```

Request:

```json
{
  "message": "이 작품을 밝고 신나는 분위기로 소개해줘",
  "characterId": "character_01",
  "selectedArtifactId": "artifact_classic_01",
  "mode": "guide"
}
```

Response:

```json
{
  "reply": "이 작품은 오래된 공연장의 기억을 밝고 따뜻한 분위기로 재해석한 전시물입니다.",
  "commands": [
    {
      "type": "setEmotion",
      "characterId": "character_01",
      "emotion": "happy"
    },
    {
      "type": "triggerStageEvent",
      "characterId": "character_01",
      "eventName": "style_classic"
    }
  ],
  "retrievedSources": ["artifact_classic_01"],
  "success": true,
  "latencyMs": 850
}
```

## 6. 전시물 Knowledge

초기에는 3개 정도면 충분하다.

```json
{
  "id": "artifact_classic_01",
  "title": "Classic Memory Frame",
  "zone": "main_hall",
  "summary": "오래된 무대 기억을 표현한 전시물",
  "description": "빛바랜 금속 프레임과 따뜻한 조명을 통해 과거 공연장의 분위기를 재해석한 작품입니다.",
  "tags": ["classic", "aged", "memory", "stage"],
  "recommendedEmotion": "happy",
  "recommendedStageEvent": "style_classic",
  "guideTone": "따뜻하고 설명적인 톤"
}
```

추천 문서:

- `artifact_classic_01`: Classic/Aged 스타일과 연결
- `artifact_modern_01`: Modern/clean 스타일과 연결
- `artifact_stage_01`: 무대, 조명, 캐릭터 반응과 연결

## 7. RAG 전략

MVP에서는 별도 Vector DB를 바로 띄우지 않는다. 대신 in-memory 검색으로 시작한다.

1단계:

- JSON 파일 로드
- title, summary, description, tags를 검색 텍스트로 구성
- selectedArtifactId 우선 반영
- keyword score로 관련 문서 1~3개 선택

2단계:

- embedding API 추가
- 문서 embedding cache 저장
- cosine similarity 검색

3단계:

- 필요하면 Qdrant/Chroma 도입

지원서 관점에서는 2단계까지만 해도 `RAG`, `embedding`, `retrieval`, `context injection`을 설명할 수 있다. Qdrant/Chroma는 시간이 남을 때 넣는 보강이다.

## 8. Prompt와 Structured Output

LLM에는 일반 답변만 요구하지 않는다. 반드시 실행 가능한 JSON을 요구한다.

프롬프트 역할:

```text
너는 Unreal Engine 전시 공간을 안내하는 AI 가이드다.
사용자의 한국어 질문을 읽고, 전시물 설명과 필요한 실행 명령을 JSON으로 반환한다.
허용되지 않은 command type은 만들지 않는다.
애매한 요청이면 commands는 빈 배열로 둔다.
```

허용 command:

- `setEmotion`
- `playAnimation`
- `triggerStageEvent`

허용 emotion:

- `happy`
- `sad`
- `angry`
- `surprise`
- `neutral`

허용 animation:

- `wave`
- `bow`
- `clap`
- `explain`
- `idle`

허용 stage event:

- `style_classic`
- `style_aged`
- `style_modern`
- `spotOn`
- `spotOff`

검증 규칙:

- JSON parse 실패 시 fallback
- command type whitelist
- enum whitelist
- 요청당 command 최대 3개
- message 최대 300자
- characterId 기본값은 `character_01`
- 알 수 없는 명령은 실행하지 않음

## 9. 로그와 데이터셋

대화 로그는 LLM Engineer 지원에서 중요하다. 단순 기능 구현이 아니라 개선 루프를 보여줄 수 있기 때문이다.

저장 예시:

```json
{
  "timestamp": "2026-04-26T12:00:00Z",
  "userText": "이 작품을 설명해줘",
  "selectedArtifactId": "artifact_classic_01",
  "retrievedSources": ["artifact_classic_01"],
  "llmReply": "이 작품은...",
  "generatedCommands": [],
  "executedCommands": [],
  "success": true,
  "latencyMs": 850,
  "promptVersion": "guide-v1"
}
```

활용:

- 실패 케이스 분석
- prompt 개선
- evaluation JSONL 생성
- fine-tuning dataset 후보 생성

실제 fine-tuning은 이번 단계에서 하지 않는다. 대신 fine-tuning 가능한 데이터 구조를 남긴다.

## 10. 개발 순서

1. Chat DTO와 Controller 추가
2. rule/keyword fallback 구현
3. 전시물 knowledge JSON 작성
4. in-memory knowledge search 구현
5. LLM client 인터페이스 추가
6. prompt builder 작성
7. structured output parser와 validator 구현
8. 기존 command dispatcher 연결
9. conversation log JSONL 저장
10. 모바일 패널 Chat 탭 추가
11. `npm run build` 후 서버 `wwwroot` 반영
12. publish 후 노트북 실기기 테스트

## 11. Motion Generation은 어떻게 다룰지

현재 우선순위는 motion generation이 아니라 `LLM/RAG 전시 가이드 챗봇`이다.

이유:

- LLM Engineer 지원요건을 직접 보강하는 것은 챗봇/RAG/structured output이다.
- offline motion generation은 3D/Motion 보강에는 좋지만, 구현 리스크가 높다.
- 생성된 모션을 Unreal에 넣으면 결국 Mixamo 애니메이션처럼 asset으로 재생하는 형태에 가깝다.

후속 실험으로는 의미가 있다.

```text
Text-to-motion 모델로 wave/bow/point 같은 간단한 모션 생성
-> 좋은 결과만 선택
-> Unreal animation asset으로 import
-> 챗봇이 motionIntent로 해당 animation 선택
```

안전한 표현:

```text
LLM/RAG가 사용자 의도를 해석해 motionIntent를 생성하고,
현재는 기존 Unreal animation으로 실행하며,
향후 text-to-motion 모델로 생성한 gesture animation asset을 연결할 수 있도록 설계했다.
```

피해야 할 표현:

```text
실시간 motion generation을 구현했다.
```

## 12. 완료 기준

기능 완료:

- 모바일 Chat UI에서 질문 가능
- 전시물 설명 응답 가능
- Happy/Aged/Classic/Wave 계열 command 실행 가능
- retrieved source 표시 가능
- conversation log 저장
- 기존 control 기능 유지

기술 어필 완료:

- LLM API 사용
- prompt engineering 문서화
- structured output validation
- RAG context 사용
- command safety 구조
- log 기반 evaluation/fine-tuning dataset 확장 가능성 확보
## Streaming 및 StreamMode 보충

채팅 응답은 기존 JSON 방식과 SSE 스트리밍 방식을 병렬로 지원한다.

- 세부 문서: [AI-CHAT-STREAMING-ARCHITECTURE-PLAN.md](./AI-CHAT-STREAMING-ARCHITECTURE-PLAN.md)

핵심 결정:

- `POST /api/chat` — JSON 응답 유지 (fallback, 디버깅 용도)
- `POST /api/chat/stream` — SSE 스트리밍 (운영 기본 경로)
- 기본값은 `stream`. Mobile Panel은 기본적으로 `/api/chat/stream`을 호출한다.
- endpoint는 명시적으로 분리한다. 단일 endpoint에서 응답 형식을 동적으로 바꾸지 않는다.

StreamMode 선택 옵션 (`stream` / `json` / `auto`):

- `stream` — 기본값. 항상 SSE
- `json` — 개발자 디버깅, fallback 테스트
- `auto` — 클라이언트 side 헬퍼. 메시지 의도를 분석해 endpoint 자동 선택. 실험/개발자 옵션

`SuggestedCommands`는 streaming 중간에 실행하지 않는다. complete event 이후에만 검증하고 Unreal에 전달한다.

## AI Gateway 분리 보충

OpenAI API를 사용하는 최종 구조는 Steam 배포 시 API 키 노출을 막기 위해 로컬 서버에 직접 붙이지 않고, 별도 클라우드 Gateway로 분리한다.

- 세부 문서: [AI-GATEWAY-ARCHITECTURE-PLAN.md](./AI-GATEWAY-ARCHITECTURE-PLAN.md)
- 이 문서의 범위: 모바일 패널 챗봇 UX, 전시 가이드 기능, RAG/structured command 흐름
- Gateway 문서의 범위: 클라우드 AI 서버 분리, API 키 보안, Steam 배포 구조, 프로젝트 배치

개발 초반에는 일반 ChatGPT 대화처럼 `ExhibitionAiGateway`의 `/api/ai/chat`을 먼저 테스트한다. 이후 전시물 context, structured command, Unreal command 검증을 단계적으로 붙인다.

## Streaming 응답 계획

챗봇 UX를 강화하기 위해 텍스트 답변은 streaming으로 표시하고, Unreal command는 응답 완료 후 검증해서 실행하는 구조를 검토한다.

- 세부 문서: [AI-CHAT-STREAMING-ARCHITECTURE-PLAN.md](./AI-CHAT-STREAMING-ARCHITECTURE-PLAN.md)
- 핵심 원칙: 텍스트는 즉시 표시, command는 complete event 이후 whitelist 검증

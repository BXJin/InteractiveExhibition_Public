# AI Gateway 아키텍처 계획

작성일: 2026-04-27

## 0. 목적

이 문서는 AI 전시 가이드 챗봇에 OpenAI API 같은 외부 LLM API를 붙일 때, Steam 배포와 API 키 보안을 동시에 만족하기 위한 서버 구조를 정리한다.

현재 `Server-AspNet/ExhibitionServer`는 로컬 실행 서버다. 모바일 패널 입력을 받고, Unreal 클라이언트와 실시간으로 통신하며, 이동/회전/감정/맵 변경 같은 명령을 중계한다. 이 서버는 패키징된 Unreal 실행 파일과 함께 사용자 PC에서 실행된다.

OpenAI API 키를 이 로컬 서버에 포함하면 Steam 배포 파일에서 추출될 수 있다. 따라서 LLM 호출은 별도 클라우드 서버인 `ExhibitionAiGateway`로 분리한다.

## 1. 최종 구조

```text
Mobile Panel
-> Local ExhibitionServer
-> Cloud ExhibitionAiGateway
-> OpenAI API

Cloud ExhibitionAiGateway
-> reply + suggestedCommands 반환

Local ExhibitionServer
-> suggestedCommands 검증
-> Unreal Client에 허용된 명령만 전달
```

역할 분리는 다음과 같다.

| 구성 | 실행 위치 | 역할 |
|---|---|---|
| `Mobile-Panel` | 사용자 브라우저 | 채팅 입력, 조이스틱, 버튼 UI |
| `ExhibitionServer` | 사용자 PC 로컬 | 모바일 패널 제공, Unreal 연결, 실시간 명령 중계, LLM 명령 검증 |
| `ExhibitionAiGateway` | 클라우드 | OpenAI API 호출, 프롬프트 관리, LLM 응답 생성, 사용량 제한 |
| `OpenAI API` | 외부 서비스 | 자연어 응답 및 구조화 결과 생성 |

## 2. 왜 로컬 서버에 OpenAI를 직접 넣지 않는가

`ExhibitionServer`는 Steam 빌드에 포함되는 로컬 실행 파일이다. 여기에 API 키를 넣으면 다음 문제가 생긴다.

- `appsettings.json`, `.env`, DLL, 실행 파일, 프론트 번들에서 키가 노출될 수 있다.
- 사용자가 배포 파일을 분석하면 키를 추출할 가능성이 있다.
- 키가 유출되면 비용 폭탄, 계정 제한, 악용 요청 문제가 생긴다.
- 로컬 실시간 제어 서버와 외부 AI 운영 책임이 섞인다.

따라서 로컬 서버는 AI를 직접 수행하지 않고, AI Gateway를 호출하는 클라이언트 역할만 맡는다.

## 3. 프로젝트 배치

새 프로젝트는 같은 솔루션 안에 추가하되, 배포 단위는 분리한다.

```text
InteractiveExhibition.sln

Client-Unreal/
  ExhibitionClient/

Mobile-Panel/

Server-AspNet/
  ExhibitionServer/

Cloud-AI-Gateway/
  ExhibitionAiGateway/

Shared/
  Exhibition.Shared/
```

같은 솔루션에 두는 이유:

- 같은 제품의 일부이므로 공통 계약을 공유하기 쉽다.
- `Shared/Exhibition.Shared`의 DTO를 재사용할 수 있다.
- 전체 빌드 검증이 쉽다.
- 포트폴리오에서 로컬 제어 서버와 클라우드 AI 서버의 분리 구조를 설명하기 좋다.

단, 배포는 프로젝트별로 한다.

```powershell
dotnet publish Server-AspNet/ExhibitionServer/ExhibitionServer.csproj
dotnet publish Cloud-AI-Gateway/ExhibitionAiGateway/ExhibitionAiGateway.csproj
```

## 4. 보안 원칙

Steam 배포 파일에는 OpenAI API 키가 절대 포함되면 안 된다.

금지:

- `appsettings.json`에 API 키 저장
- `.env` 파일을 Steam 빌드에 포함
- Vite/React 프론트 번들에 API 키 주입
- Unreal 설정 파일에 API 키 저장
- 로컬 서버 실행 파일에 API 키 하드코딩

허용:

- 개발 PC에서는 환경변수 또는 .NET User Secrets 사용
- 클라우드 Gateway에서는 클라우드 Secret/환경변수 사용
- Steam 빌드에는 `AI_GATEWAY_URL` 같은 공개 가능한 URL만 포함

권장 설정:

```text
OPENAI_API_KEY          클라우드 Gateway 전용 Secret
AI_GATEWAY_URL          로컬 서버가 호출할 공개 Gateway 주소
AI_GATEWAY_CLIENT_KEY   필요 시 Gateway 접근 제어용 제한 키
```

`AI_GATEWAY_CLIENT_KEY`는 OpenAI 키가 아니다. 유출되어도 OpenAI 계정 전체가 노출되지 않도록 Gateway에서 rate limit, origin/session 검증, quota를 둔다.

## 5. API 흐름

초기 테스트 단계에서는 일반 ChatGPT 대화처럼 동작하게 만든다.

```http
POST /api/ai/chat
```

Request:

```json
{
  "message": "안녕, 너는 누구야?",
  "conversationId": "local-session-001"
}
```

Response:

```json
{
  "reply": "안녕하세요. 저는 전시 안내를 도와주는 AI 가이드입니다.",
  "suggestedCommands": [],
  "success": true,
  "latencyMs": 920
}
```

전시 가이드 단계에서는 관련 전시 데이터와 명령 후보를 포함한다.

```json
{
  "message": "이 작품을 밝고 신나는 느낌으로 소개해줘",
  "selectedArtifactId": "artifact_classic_01",
  "retrievedContext": [
    {
      "id": "artifact_classic_01",
      "title": "Classic Memory Frame",
      "summary": "오래된 기억과 클래식한 분위기를 표현한 전시물"
    }
  ]
}
```

응답은 LLM이 자유 텍스트만 반환하지 않고, 검증 가능한 구조를 포함해야 한다.

```json
{
  "reply": "이 작품은 오래된 기억을 밝고 경쾌한 분위기로 다시 보여주는 전시물입니다.",
  "suggestedCommands": [
    {
      "type": "setEmotion",
      "characterId": "character_01",
      "emotion": "happy"
    },
    {
      "type": "playAnimation",
      "characterId": "character_01",
      "animation": "explain"
    }
  ],
  "success": true,
  "latencyMs": 1100
}
```

## 6. 명령 안전성

LLM 응답은 절대 Unreal에 바로 실행하지 않는다.

실행 기준:

- Local `ExhibitionServer`가 명령 타입 whitelist를 검사한다.
- enum 값도 whitelist를 통과해야 한다.
- 한 번의 채팅에서 실행 가능한 명령 수를 제한한다.
- 이동/회전 같은 실시간 제어 명령은 LLM 명령 대상에서 제외한다.
- 파일 실행, 네트워크 설정 변경, 임의 콘솔 명령 같은 기능은 LLM 명령으로 허용하지 않는다.

초기 허용 명령:

```text
setEmotion: happy, sad, angry, surprise, neutral
playAnimation: wave, bow, clap, explain, idle
triggerStageEvent: style_classic, style_aged, style_modern, spotOn, spotOff
```

이동/회전/카메라 조작은 기존 모바일 패널의 직접 입력 흐름을 유지한다.

```text
Joystick / Rotate input
-> Local ExhibitionServer
-> Unreal Client
```

LLM은 실시간 조작을 대신하지 않고, 전시 가이드/감정/제스처/무대 분위기 제안에 집중한다.

## 7. 구현 단계

### Phase 1. Gateway 기본 생성

- `Cloud-AI-Gateway/ExhibitionAiGateway` ASP.NET Core Web API 프로젝트 추가
- `InteractiveExhibition.sln`에 프로젝트 포함
- `Shared/Exhibition.Shared` 참조 연결
- `POST /api/ai/chat` 추가
- 환경변수 `OPENAI_API_KEY`로 OpenAI API 호출
- 일반 대화 테스트

### Phase 2. 로컬 서버 연동

- `ExhibitionServer`에 `IAiGatewayClient` 추가
- `/api/chat`에서 기존 rule fallback 대신 Gateway 호출 우선 적용
- Gateway 실패 시 기존 로컬 rule/knowledge 응답으로 fallback
- 응답 latency, success, error reason 로그 기록

### Phase 3. 전시 RAG 적용

- 전시물 JSON을 검색해 context 생성
- context를 Gateway 요청에 포함
- 프롬프트 버전 관리
- retrieved source와 응답 품질 로그 기록

### Phase 4. Structured Command 적용

- Gateway가 `reply`와 `suggestedCommands`를 구조화해서 반환
- Local `ExhibitionServer`가 command whitelist 검증
- 검증 통과 명령만 Unreal에 전달
- 실패 명령은 로그로만 남기고 실행하지 않음

### Phase 5. Steam 배포 정리

- Steam 빌드에서 API 키 제거 확인
- `.pdb`, 로그, development 설정 제거
- `AI_GATEWAY_URL`만 포함
- Gateway rate limit, timeout, fallback, privacy notice 정리
- Steam Content Survey의 AI 사용 항목에 live-generated AI 여부와 guardrail 설명

## 8. 포트폴리오 설명 포인트

이 구조는 단순히 ChatGPT API를 호출하는 기능보다 실무적으로 설명하기 좋다.

강조할 수 있는 내용:

- Steam 배포 환경에서 API 키가 노출되지 않도록 로컬 서버와 클라우드 AI Gateway를 분리했다.
- LLM 응답을 Unreal 명령으로 바로 실행하지 않고, command schema와 whitelist로 검증했다.
- 전시물 데이터를 RAG context로 구성해 사용자 질문에 맞는 가이드 응답을 생성한다.
- LLM 장애나 네트워크 실패 시 로컬 rule-based fallback으로 기본 기능을 유지한다.
- 대화 로그를 평가/fine-tuning dataset 후보로 남길 수 있는 구조를 설계했다.

지원서 표현 예시:

```text
Unreal 기반 인터랙티브 전시 프로젝트에 LLM/RAG 기반 AI 가이드 서버를 설계했습니다.
Steam 배포 환경에서 API Key 노출을 방지하기 위해 로컬 실시간 제어 서버와 클라우드 LLM Gateway를 분리했고,
LLM 출력은 structured command schema로 제한한 뒤 서버 측 whitelist 검증을 통과한 명령만 Unreal에 전달하도록 설계했습니다.
```

## 9. 현재 결정

현재 프로젝트에서는 다음 방향을 채택한다.

- `ExhibitionServer`는 로컬 실시간 제어 서버로 유지한다.
- OpenAI API 연동은 `ExhibitionAiGateway`에서 담당한다.
- 같은 솔루션 안에 새 프로젝트를 추가하되, 배포는 프로젝트별로 분리한다.
- 초기 구현은 일반 대화 테스트부터 시작한다.
- 전시물 RAG, structured command, Unreal 명령 검증은 단계적으로 추가한다.

## 10. Streaming 확장 계획

챗봇 체감 응답 속도를 높이기 위해 streaming endpoint를 별도로 추가하는 방향을 채택한다. 기존 JSON endpoint는 fallback과 디버깅을 위해 유지하고, streaming은 `/api/ai/chat/stream` 및 `/api/chat/stream`으로 확장한다.

자세한 설계는 [AI-CHAT-STREAMING-ARCHITECTURE-PLAN.md](./AI-CHAT-STREAMING-ARCHITECTURE-PLAN.md)를 참고한다.
## 2026-05-02 RAG/Embedding 배포 구조 갱신

Steam 배포 기준에서는 `ExhibitionServer`에 OpenAI API key가 들어가면 안 된다. 따라서 embedding API 호출은 로컬 서버에서 제거하고, 클라우드 `ExhibitionAiGateway`에서만 수행하도록 구조를 정리한다.

현재 구조:

```text
Mobile Panel
-> Local ExhibitionServer
   -> 전시물 JSON 보유
   -> catalog / selectedArtifact / keyword / alias 기반 후보 context 선택
   -> OpenAI API 직접 호출 없음
-> Cloud ExhibitionAiGateway
   -> 후보 context embedding rerank
   -> OpenAI Responses API 호출
   -> reply + suggestedCommands 반환
-> Local ExhibitionServer
   -> command whitelist 검증
   -> Unreal command dispatch
```

역할 분리:

- `ExhibitionServer`: 전시 runtime의 주인. Unreal 상태, 전시물 JSON, 모바일 패널, 로컬 command 검증을 담당한다.
- `ExhibitionAiGateway`: OpenAI key가 필요한 inference 작업의 주인. embedding rerank, LLM 호출, provider 교체, rate limit을 담당한다.

이 구조의 장점:

- Steam 빌드에 `OPENAI_API_KEY`가 포함되지 않는다.
- 전시물 원본 데이터는 로컬 패키지와 같은 버전을 유지한다.
- Gateway가 전시물 DB 전체를 복제하지 않아도 된다.
- OpenAI embedding은 Azure 같은 클라우드 서버에서만 호출된다.
- 추후 Gateway에 cache, Vector DB, rerank metric을 추가할 수 있다.

현재 구현 상태:

- 로컬 `ExhibitionServer`의 OpenAI embedding client는 제거했다.
- 로컬 `KnowledgeSearch:EmbeddingsEnabled` 기본값은 `false`다.
- 로컬은 후보 context를 최대 8개까지 Gateway에 전달한다.
- Gateway는 `Rag` 옵션을 기준으로 `text-embedding-3-small` embedding rerank를 수행하고, 상위 context만 LLM prompt에 넣는다.

Azure App Service 환경변수:

```text
OPENAI_API_KEY=sk-...
AI_GATEWAY_CLIENT_KEY=긴 랜덤 문자열
OPENAI_MODEL=gpt-4o-mini
OPENAI_EMBEDDING_MODEL=text-embedding-3-small
```

Steam/로컬 서버 환경변수:

```text
AI_GATEWAY_URL=https://<gateway-name>.azurewebsites.net
AI_GATEWAY_CLIENT_KEY=Gateway와 동일한 값
```

Steam/로컬 서버에는 `OPENAI_API_KEY`를 넣지 않는다.

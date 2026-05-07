# 05. Blue Garage Role Fit

이 문서는 Blue Garage 공고 기준으로 `InteractiveExhibition` 프로젝트가 어떤 방식으로 연결되는지 정리한 자료다. 단순히 기술명을 나열하기보다, 프로젝트 안에서 어떻게 적용했는지와 아직 부족한 점을 함께 적는다.

## LLM Engineer 연결 포인트

### AI API 연동

이 프로젝트는 OpenAI API를 직접 클라이언트에 붙이지 않고, Azure App Service에 배포한 `ExhibitionAiGateway`를 통해 호출한다.

적용 방식:

- Windows 패키징 앱에는 OpenAI API Key를 포함하지 않음
- Azure 환경 변수에만 `OPENAI_API_KEY` 저장
- 로컬 서버는 `AiGatewayClient`로 Gateway에만 요청
- Gateway는 provider abstraction을 통해 OpenAI 호출 처리

성과:

- 다른 PC에서 배포 빌드를 실행해도 AI 답변 가능
- API Key가 itch.io/Steam 배포 파일에 포함되지 않음
- Provider 교체 구조를 만들어 Gemini/Groq/Local LLM 확장 여지 확보

부족한 점:

- 운영 수준의 사용자 인증은 아직 client key 기반
- 장기적으로 Steam ticket, user session token, per-user quota가 필요

## Prompt Engineering

### 적용 방식

AI가 단순히 자유 대화만 하는 것이 아니라, 전시 가이드 역할을 수행하고 Unreal에서 실행 가능한 명령을 함께 제안하도록 prompt와 응답 구조를 설계했다.

예시:

- 사용자가 "트리케라톱스 설명해줘"라고 입력
- 서버가 관련 전시물 context를 찾아 Gateway에 전달
- AI는 설명 텍스트와 함께 `explain` 같은 캐릭터 애니메이션 명령 후보를 반환
- 서버는 허용된 명령만 Unreal로 전달

성과:

- 답변 텍스트와 3D 인터랙션이 분리되지 않고 하나의 체험으로 연결됨
- AI가 직접 시스템을 마음대로 제어하지 않도록 command boundary를 둠

부족한 점:

- Prompt version 관리와 평가 데이터셋은 아직 부족
- 실패 케이스별 prompt regression test가 필요

## RAG / Knowledge Grounding

### 적용 방식

전시물 설명을 코드에 하드코딩하지 않고 Knowledge JSON으로 분리했다. 각 전시물은 description, alias, tag, source metadata를 갖고, 로컬 서버는 질문과 관련 있는 후보 문서를 찾는다.

현재 구조:

```text
User Question
  -> Local Knowledge Search
  -> Candidate Context
  -> AI Gateway Embedding Rerank
  -> OpenAI Answer
```

성과:

- 전시물이 늘어나도 JSON 데이터 추가로 확장 가능
- 사용자가 공식 명칭이 아닌 별칭으로 질문해도 매칭 가능
- Vector DB로 교체 가능한 interface 구조 확보

부족한 점:

- 외부 Vector DB는 아직 사용하지 않음
- 전시물 수가 많아지면 Qdrant, pgvector, Azure AI Search 같은 저장소가 필요
- retrieval quality 평가 지표가 아직 없음

## Streaming Inference

### 적용 방식

AI 응답을 한 번에 기다리지 않고 streaming으로 모바일 패널에 전달한다.

```text
OpenAI Streaming
  -> Azure AI Gateway
  -> Local ExhibitionServer
  -> Mobile Panel
```

성과:

- 사용자가 답변을 기다리는 체감 시간이 줄어듦
- 긴 전시 설명도 즉시 출력이 시작되어 UX가 좋아짐

부족한 점:

- Streaming 중 command 실행 시점과 final structured output 처리 정책을 더 정교하게 다듬을 수 있음
- token-level latency와 error recovery 로깅이 더 필요

## Multi-turn Conversation

### 적용 방식

사용자가 "이거 더 자세히 설명해줘"처럼 이전 질문을 전제로 말할 수 있도록 conversation history를 AI 요청에 포함했다.

성과:

- 단발성 Q&A보다 실제 전시 가이드에 가까운 대화 가능
- 전시물 설명 이후 후속 질문을 자연스럽게 처리 가능

부족한 점:

- 세션 저장은 아직 장기 persistent memory 수준은 아님
- 개인정보/로그 보존 정책은 별도 설계 필요

## Data Preprocessing / Knowledge Authoring

### 적용 방식

Smithsonian 3D 전시물 후보를 프로젝트에 넣고, 각 전시물의 설명과 alias/tag를 AI 검색용 데이터로 정리했다.

성과:

- 전시물 데이터와 Unreal outliner 이름을 맞춰 실제 3D 오브젝트와 AI 설명을 연결
- Triceratops, Bell X-1, Ritual wine container 같은 서로 다른 도메인 전시물 구성

부족한 점:

- 데이터 출처와 license metadata를 더 체계적으로 보강해야 함
- 설명 품질을 사람이 검수한 gold answer 형태로 관리하면 더 좋음

## Cloud Model Serving / MLOps 기초

### 적용 방식

AI Gateway를 Azure App Service에 배포하고, 운영 환경 변수로 model, embedding model, API key, client key를 관리했다.

성과:

- 로컬 앱과 클라우드 AI 서버를 분리
- Production 환경 변수로 OpenAI key 관리
- Gateway URL만 바꾸면 로컬 서버가 다른 AI 서버를 바라볼 수 있음

부족한 점:

- CI/CD 자동 배포는 아직 수동 zip 배포 중심
- Observability는 기본 로그 수준
- 비용 모니터링, alert, tracing, dashboard가 필요

## Fine-tuning 관련성

### 현재 적용 여부

Fine-tuning은 아직 직접 수행하지 않았다.

### 프로젝트에서 연결 가능한 부분

현재 multi-turn 대화, 사용자의 질문, AI 답변, command 성공 여부를 로그로 모으면 fine-tuning 또는 instruction tuning용 데이터셋 후보가 된다.

예시 데이터:

```json
{
  "user": "트리케라톱스 설명해줘",
  "context": "Triceratops exhibit metadata...",
  "assistant": "트리케라톱스는...",
  "command": "play_animation:explain",
  "accepted": true
}
```

부족한 점:

- 실제 fine-tuning pipeline은 없음
- 데이터 정제, 평가셋 분리, 모델 비교 실험은 별도 과제

## 3D Graphics / Motion 연결 포인트

### 적용 방식

이 프로젝트의 3D/Motion 포인트는 AI로 새로운 모션을 생성하는 것보다는, Unreal 전시장 안에서 AI 응답과 캐릭터 애니메이션을 연결하는 쪽에 가깝다.

구현된 흐름:

```text
User Prompt
  -> AI Guide Response
  -> SuggestedCommands
  -> Local Server Command Dispatch
  -> Unreal Realtime Subsystem
  -> Character Animation / Stage Change
```

성과:

- AI 채팅 결과가 화면 텍스트에서 끝나지 않고 캐릭터 행동으로 연결됨
- `explain`, `wave`, `happy`, `sad` 같은 애니메이션 상태를 AI 명령 후보와 연결
- 모바일 패널, 서버, Unreal 캐릭터가 하나의 인터랙션 루프로 동작

부족한 점:

- Text-to-motion 모델로 새 애니메이션을 생성한 것은 아님
- MDM, MotionGPT, AMASS, HumanML3D 같은 motion generation 데이터셋/모델 적용은 NextProject로 분리
- 현재는 보유 animation asset과 command mapping 중심

## Blue Garage 기준으로 강하게 어필할 수 있는 부분

- LLM API를 단순 호출이 아니라 Gateway, streaming, RAG, command safety까지 포함해 제품 구조로 연결
- Unreal 3D 프로젝트에 AI를 실제 기능으로 통합
- 로컬 실행 앱과 클라우드 AI 서버를 분리해 배포 보안을 고려
- 모바일 패널, 서버, 게임 클라이언트, 클라우드 AI를 모두 다룬 end-to-end 구현

## Blue Garage 기준으로 솔직히 보완해야 할 부분

- Fine-tuning 직접 경험
- 대규모 Vector DB 운영 경험
- LLM evaluation 체계
- MLOps observability와 자동 배포
- 실제 text-to-motion 모델 적용 경험
- 대규모 동시 사용자 처리


# Blue Garage 지원 적합도 및 AI 전시 가이드 전략

작성일: 2026-04-26

참고 공고:

- `[Blue Garage] AI Artist 2026 인턴십`: https://recruit-apply.jype.com/ko/o/159641
- `[Blue Garage] AI Artist / LLM Engineer`: https://recruit-apply.jype.com/ko/o/170391
- `[Blue Garage] AI Artist / Software Engineer / 3D Graphics, Motion`: https://recruit-apply.jype.com/ko/o/159546
- `[Blue Garage] AI Artist / AI Sound/Voice Engineer`: https://recruit-apply.jype.com/ko/o/184904

## 0. 결론

2026-04-28 기준, 가장 설득력 있는 1순위는 `LLM Engineer`로 바뀌었다.

이유:

- OpenAI Responses API 기반 AI Gateway 분리 구조 구현
- prompt engineering, structured output schema, command whitelist 검증
- in-memory keyword RAG + RetrievedContext Gateway 전달
- conversationId 기반 multi-turn memory (ConversationMemoryStore)
- OpenAI native streaming → Gateway → ExhibitionServer → 패널까지 end-to-end buffering 없음
- ReplyStreamExtractor로 JSON 스트림에서 reply 필드 실시간 추출
- JSONL 대화 로그 저장 (evaluation/fine-tuning dataset 후보)
- API key를 클라이언트/로컬 exe에서 분리한 Gateway 보안 구조

2순위는 `Software Engineer / 3D Graphics, Motion`이다.

이유:

- Unreal Engine 5 기반 실시간 3D 전시 공간
- 모바일 패널과 Unreal의 실시간 연동
- 감정, 스타일, 맵 변화, 캐릭터 반응 command 파이프라인
- ASP.NET 서버와 패키징된 Unreal 실행물 연동
- 노트북 로컬 서버와 모바일 패널 접속까지 검증

다만 motion generation, retargeting, PyTorch 기반 모델 경험이 없어 핵심 자격요건에는 약하다.

`AI Sound/Voice Engineer`는 현재 가장 약하다. TTS/ASR이 없기 때문에 주력 지원보다는 후속 확장으로만 언급하는 것이 안전하다.

## 1. 지원 전략 요약

| 분야 | 현재 적합도 | 판단 |
|---|---:|---|
| LLM Engineer | 높음 | AI Gateway, RAG, streaming, multi-turn, structured output, command safety까지 end-to-end 구현 |
| Software Engineer / 3D Graphics, Motion | 중간 | Unreal 실시간 파이프라인은 강점. motion generation 경험 없어 핵심 자격요건에 약함 |
| AI Sound/Voice Engineer | 낮음 | TTS/ASR/voice pipeline이 없어 후순위 |

핵심 전략:

```text
1순위: LLM Engineer — AI Gateway + RAG + streaming + command pipeline
2순위: Software Engineer / 3D Graphics — Unreal AI-driven 인터랙션 파이프라인
후속: embedding RAG, evaluation dataset, offline motion generation 실험
```

## 2. 현재 프로젝트에서 충분한 점

### 2.1 3D Graphics / Motion 관점

충분한 점:

- Unreal Engine 5 프로젝트가 실제로 실행됨
- 패키징된 exe에서 서버와 함께 동작하도록 구성
- 모바일 패널에서 감정/스타일/공간 변화를 실행
- `Happy`, `Aged`, `Classic` 같은 반응이 이미 검증됨
- 실시간 입력이 캐릭터와 전시 공간 반응으로 이어지는 end-to-end 구조가 있음

지원서에서 말할 수 있는 핵심:

```text
모바일 입력과 서버 command pipeline을 통해 Unreal Engine 캐릭터의 감정, 모션, 전시 공간 연출을 실시간으로 제어하는 인터랙티브 전시 프로토타입을 구현했다.
```

### 2.2 LLM Engineer 관점

현재 충분한 점:

- OpenAI API 연동 및 AI Gateway 분리 구조 구현
- prompt engineering (역할 지정, JSON schema, command 제한 규칙)
- structured output + 서버 whitelist 이중 검증
- in-memory keyword RAG + RetrievedContext Gateway 전달
- conversationId 기반 multi-turn memory
- OpenAI native streaming end-to-end (buffering 없음)
- JSONL 대화 로그 (evaluation/fine-tuning dataset 후보)
- API key 보안 구조 (Gateway 분리)

아직 부족한 점:

- RAG가 keyword 기반 → embedding/cosine similarity 미구현
- evaluation dataset 없음
- token usage / 비용 로그 없음
- Gemini/Groq 실 구현 없음 (Placeholder)

## 3. 현재 부족한 점

### 3.1 LLM Engineer 부족 요소

구현 완료:

- LLM API 연동 (OpenAI Responses API)
- prompt engineering (JSON schema, command 규칙, 역할 지정)
- structured output + command whitelist 검증
- in-memory keyword RAG + context injection
- multi-turn memory (ConversationMemoryStore)
- native streaming end-to-end
- conversation log JSONL 저장

아직 부족한 점:

- embedding 기반 RAG (cosine similarity) 미구현
- evaluation dataset 없음
- token usage / 비용 로그 없음
- Gemini/Groq provider 실 구현 없음
- MLOps, 배포 자동화, 모니터링 없음

### 3.2 3D/Motion 부족 요소

부족한 점:

- motion generation 모델을 직접 사용하지 않음
- AMASS, HumanML3D 같은 motion dataset 처리 경험 없음
- rigging, IK, retargeting 구현은 제한적
- 생성 motion을 Unreal skeleton에 적용한 경험은 아직 없음

다만 이것이 당장 치명적인 약점은 아니다. 현재 프로젝트는 “motion generation 모델”보다는 “실시간 3D 캐릭터 제어 파이프라인”에 강점이 있다.

안전한 표현:

```text
현재는 AI motion generation을 직접 구현한 단계는 아니지만, 자연어/모바일 입력을 Unreal animation event와 stage command로 변환하는 실시간 실행 파이프라인을 구축했다.
```

### 3.3 Sound/Voice 부족 요소

부족한 점:

- TTS 없음
- ASR 없음
- emotional voice 없음
- voice cloning 없음
- turn-taking 없음
- streaming audio pipeline 없음

이 분야는 지금은 주력으로 밀지 않는 편이 맞다.

## 4. AI 전시 가이드로 보강할 내용

추가할 기능:

```text
전시 공간 안에 3~5개 전시물 배치
-> 모바일 패널에서 전시물 질문
-> 서버가 전시물 knowledge 검색
-> LLM이 답변과 command를 structured output으로 생성
-> 서버가 command 검증
-> Unreal 캐릭터/공간 반응 실행
-> 대화 로그 저장
```

이 기능이 들어가면 LLM Engineer 지원요건과 직접 연결된다.

연결되는 키워드:

- LLM API
- prompt engineering
- RAG
- embedding
- vector search
- structured output
- command validation
- data preprocessing
- conversation log
- evaluation dataset
- fine-tuning dataset 후보

## 5. “처음엔 하지 말자”에서 “지원용으로 넣자”로 바뀐 항목

아래 항목은 순수 MVP만 보면 과하다. 하지만 지원 경쟁력을 위해 일부러 넣는 것이 좋다.

| 항목 | 처음 판단 | 지원 관점 판단 |
|---|---|---|
| LLM API | MVP 안정성만 보면 없어도 됨 | LLM Engineer 어필에 필요 |
| RAG | 전시 데이터가 작으면 keyword search로 충분 | 공고 키워드와 직접 연결되므로 넣는 게 좋음 |
| Vector DB | 로컬 시연 복잡도 증가 | 시간이 있으면 Qdrant/Chroma로 보강 |
| Fine-tuning | 지금 실제 학습은 비추천 | 로그를 dataset 후보로 저장하는 구조는 필요 |
| Motion generation | 바로 붙이면 리스크 큼 | offline 실험으로 3D/Motion 보강 가능 |
| TTS/ASR | MVP 제외 | Sound/Voice 후속 확장으로만 언급 |

정리하면, 실제 구현 우선순위는 `LLM/RAG 챗봇`이 먼저이고, `fine-tuning`, `Vector DB`, `motion generation`, `TTS`는 후속 보강이다.

## 6. Motion Generation 개념 정리

공고에서 말하는 motion generation은 단순히 Mixamo 애니메이션을 가져와 재생하는 것과 다르다.

의미:

```text
텍스트, 음성, 감정, 음악 같은 입력
-> 모델이 pose sequence 또는 motion representation 생성
-> skeleton/character에 적용
-> animation으로 재생
```

관련 모델:

- `MDM`: diffusion 기반 text-to-motion 계열
- `MotionGPT`: 언어 모델 방식으로 motion token을 다루는 계열
- `T2M-GPT`: text-to-motion generation 계열
- `MoMask`: motion token/masked modeling 계열

관련 데이터셋:

- `HumanML3D`: 자연어 설명과 사람 모션이 짝지어진 데이터셋
- `AMASS`: 여러 motion capture 데이터를 통합한 대규모 인체 모션 데이터셋

현재 프로젝트와의 관계:

- 현재는 motion generation 모델을 쓰고 있지 않다.
- 대신 모바일/자연어 입력을 Unreal animation command로 바꾸는 실행 파이프라인이 있다.
- 나중에 `motionIntent`를 추가하면 기존 animation과 생성 animation을 같은 방식으로 선택할 수 있다.

## 7. Motion Generation은 언제 하는 것이 좋은가

지금 바로 실시간 motion generation을 붙이는 것은 비추천이다.

이유:

- PyTorch/CUDA 환경이 필요할 수 있음
- 생성 결과를 Unreal skeleton에 맞추는 변환이 어렵다
- retargeting, root motion, foot sliding 문제가 생길 수 있다
- 런타임 생성은 지연시간과 안정성 문제가 크다
- 패키징 시연 안정성을 해칠 수 있다

대신 후속 실험은 의미가 있다.

현실적인 방식:

```text
오프라인 text-to-motion 생성
-> wave/bow/point 같은 간단한 모션만 선택
-> FBX 또는 Unreal animation asset으로 변환
-> 챗봇이 motionIntent로 해당 animation 실행
```

추천 모션:

- 손 흔들기
- 고개 숙여 인사
- 가리키기
- 설명 제스처
- 박수
- idle happy/sad

피하는 편이 좋은 모션:

- 긴 걷기/달리기
- 춤
- 점프
- 앉기/일어서기
- 물체 상호작용
- 바닥 접촉이 중요한 복잡한 동작

## 8. 구현 우선순위

지원 전 가장 현실적인 순서:

1. Chat UI 추가
2. Chat API 추가
3. 전시물 knowledge JSON 3~5개 작성
4. rule/keyword fallback 구현
5. LLM API 연동
6. prompt template 작성
7. structured output schema 설계
8. command validation 구현
9. in-memory RAG 또는 embedding search 구현
10. conversation log JSONL 저장
11. 챗봇 응답으로 Unreal 감정/모션/공간 명령 실행
12. 시연 영상 촬영
13. 시간이 남으면 offline AI-generated motion 실험

## 9. 지원서 표현

3D/Motion 중심 표현:

```text
Unreal Engine 기반 전시 공간과 모바일 패널을 ASP.NET 실시간 서버로 연결하고, 사용자 입력을 캐릭터 감정, 모션 이벤트, 공간 연출 command로 변환하는 인터랙티브 전시 프로토타입을 구현했습니다.
```

LLM Engineer 보강 후 표현:

```text
전시물 knowledge base와 LLM/RAG 기반 AI 가이드를 추가해, 사용자의 자연어 질문을 전시 설명과 structured command로 변환하고, 서버 검증 후 Unreal 캐릭터와 전시 공간이 실시간으로 반응하도록 설계했습니다.
```

motion generation 관련 안전한 표현:

```text
현재는 motion generation 모델을 직접 배포한 단계는 아니지만, LLM이 생성한 motionIntent를 기존 Unreal animation으로 실행하고, 향후 text-to-motion 모델로 생성한 gesture animation asset을 연결할 수 있도록 구조를 분리했습니다.
```

피해야 할 표현:

```text
실시간 motion generation을 구현했습니다.
```

## 10. 최종 판단

현재 프로젝트만으로도 `Software Engineer / 3D Graphics, Motion` 지원에는 충분히 연결점이 있다.

하지만 `LLM Engineer`까지 노리려면 챗봇이 반드시 필요하다. 단순 채팅 UI가 아니라, 전시물 knowledge, RAG, prompt engineering, structured output, command validation, conversation log까지 포함해야 한다.

가장 좋은 다음 단계:

```text
AI Exhibition Guide Chatbot 구현
-> LLM/RAG/structured command 검증
-> 시연 영상 확보
-> 이후 offline motion generation 실험으로 3D/Motion 보강
```
## AI Gateway 분리와 Steam 배포 보안

LLM Engineer 어필을 위해 OpenAI API 기반 AI 전시 가이드는 구현 대상에 포함한다. 다만 Steam 배포 파일에 API 키가 포함되면 안 되므로, 로컬 제어 서버인 `ExhibitionServer`에 OpenAI 키를 직접 넣지 않는다.

최종 구조는 다음과 같이 분리한다.

```text
Mobile Panel
-> Local ExhibitionServer
-> Cloud ExhibitionAiGateway
-> OpenAI API
```

이 구조는 다음 역량을 직접 보여준다.

- LLM API 연동
- prompt engineering
- RAG context 구성
- structured output 설계
- command whitelist 검증
- API key 보안
- Steam 배포 환경을 고려한 서버 분리

세부 설계는 [AI-GATEWAY-ARCHITECTURE-PLAN.md](./AI-GATEWAY-ARCHITECTURE-PLAN.md)에 정리한다.
## 2026-04-28 최신 구현 기준 Role Fit 업데이트

이 섹션은 현재 프로젝트에 실제 구현된 기능을 기준으로 Blue Garage의 `LLM Engineer`와 `Software Engineer / 3D Graphics, Motion` 공고 항목에 어떻게 연결되는지 정리한다.

### LLM Engineer 담당업무 매핑

공고상 담당업무 요약:

- LLM을 활용한 AI 아티스트 페르소나 구현 및 프롬프트 엔지니어링
- RAG 및 메모리 아키텍처를 통한 장기 기억/맥락 유지 시스템 구축
- 저지연 챗봇 파이프라인 설계 및 최적화
- 채팅/음성 등 소통 채널에 맞는 응답 모듈 개발
- 품질 평가 체계 구축 및 지속적 개선
- 최신 LLM 연구 동향 리서치 및 서비스 적용 가능성 검토

현재 프로젝트 적용:

- `ExhibitionAiGateway`를 별도 AI Gateway 프로젝트로 분리하여 OpenAI API 호출, provider 추상화, API key 보호 구조를 구현했다.
- `OpenAiResponsesProvider`에서 전시 가이드 역할, structured output schema, 안전한 command 목록을 prompt에 포함한다.
- `/api/chat`과 `/api/chat/stream`을 분리하여 JSON 응답과 SSE streaming 응답을 모두 지원한다.
- OpenAI Responses API의 native streaming을 사용하고, `ReplyStreamExtractor`로 JSON 응답 안의 `reply` 필드를 실시간 추출한다.
- `ConversationMemoryStore`로 `conversationId` 기반 최근 대화 이력을 유지하고, Gateway 요청의 `ConversationHistory`로 전달한다.
- 전시 지식 검색 결과를 `RetrievedContext`로 Gateway에 전달하여 RAG context injection 구조를 만들었다.
- LLM이 제안한 command는 `ExhibitionServer`에서 whitelist 검증 후 Unreal command로 변환한다.
- `ConversationLogger`가 대화 로그를 JSONL 형태로 저장하여 추후 evaluation/fine-tuning dataset의 기반이 된다.

현재 부족한 점:

- RAG가 아직 embedding/vector DB 기반이 아니라 keyword/in-memory 검색 중심이다.
- 장기 메모리는 아직 DB/vector memory가 아닌 서버 in-memory 세션 메모리다.
- evaluation dataset이 아직 명시적으로 없다. 질문, 기대 답변, 기대 command, 실제 결과를 비교하는 평가셋이 필요하다.
- inference 비용 최적화, provider별 비용 비교, token usage logging이 아직 없다.
- MLOps 파이프라인은 아직 구현되지 않았다. 배포 자동화, 모니터링, tracing, alerting이 보강되면 더 좋다.
- Python/PyTorch 기반 파인튜닝이나 오픈소스 LLM 학습 경험은 현재 프로젝트에는 직접 포함되어 있지 않다.

LLM Engineer 기준 현재 강점:

- 단순 ChatGPT API 호출이 아니라 `RAG context + multi-turn memory + native streaming + structured command + command validation + Unreal execution`까지 이어지는 end-to-end AI service pipeline이다.
- 엔터테인먼트/버추얼 IP/팬덤 도메인과 유사하게, AI 가이드가 사용자의 대화에 반응하고 캐릭터 감정/애니메이션/무대 상태를 제어한다.
- API key를 클라이언트에 넣지 않고 Gateway로 분리한 구조는 실제 서비스 배포 관점에서 설명하기 좋다.

LLM Engineer 보강 우선순위:

1. 전시물 3~5개와 knowledge JSON 정리
2. OpenAI `text-embedding-3-small` 기반 embedding RAG 추가
3. cosine similarity, topK, score threshold, selectedArtifact boost 구현
4. 질문/기대 답변/기대 command 평가셋 20개 작성
5. latency, provider, model, retrieved source score, command success 여부 로그 강화
6. Cloud AI Gateway 배포 및 rate limit/timeout/비용 제한 정리

### LLM Engineer 자격요건/우대사항 매핑

공고상 자격요건/우대사항 요약:

- OpenAI, Anthropic 등 API 활용 및 프롬프트 엔지니어링 경험
- RAG 시스템 설계 및 구현 경험
- 벡터 DB, 임베딩 경험
- 저지연 실시간 AI 서비스 개발 경험
- 오픈소스 LLM 파인튜닝 및 ML 모델 훈련 경험
- inference 속도 최적화 및 비용 효율화 경험
- MLOps 파이프라인 구축 경험
- 엔터테인먼트/버추얼 IP/팬덤 도메인 관심 또는 경험
- Python/PyTorch 숙련 및 비동기 처리 역량
- 최신 논문 이해 및 구현 역량

현재 충족:

- OpenAI API 활용: 충족
- 프롬프트 엔지니어링: 충족
- structured output schema: 충족
- RAG context injection: 부분 충족
- native streaming: 충족
- 멀티턴 memory architecture: 부분 충족
- 비동기 처리: 충족. ASP.NET async, SSE, Channel 기반 streaming 사용
- 엔터테인먼트/버추얼 IP 유사 도메인: 부분 충족. Unreal 캐릭터/전시 가이드/감정/애니메이션 제어로 연결
- command safety/validation: 충족

현재 미충족 또는 약함:

- embedding/vector DB: 미충족
- 오픈소스 LLM fine-tuning: 미충족
- ML 모델 훈련: 미충족
- MLOps: 미충족
- inference 비용 최적화: 약함
- 최신 논문 구현: 미충족

지원서에서 표현할 때의 핵심 문장:

```text
OpenAI Responses API 기반 AI Gateway를 분리 설계하고, RAG context, conversation memory, native SSE streaming, structured output schema, command whitelist 검증을 통해 Unreal Engine 전시 캐릭터를 안전하게 제어하는 AI 전시 가이드 파이프라인을 구현했습니다.
```

### Software Engineer / 3D Graphics, Motion 담당업무 매핑

공고상 담당업무 요약:

- Offline/real-time rendering을 위한 3D character animation & motion 생성 기술 개발 및 최적화
- 텍스트/음성/감정 등 다양한 modality와 호환되는 animation & motion 생성 기술 연구개발
- in-the-wild 데이터 정제/가공을 통한 foundation model 구축
- real-time animation 생성 및 AI 시스템과의 통합
- Unreal, Unity 등 게임/렌더링 엔진 기반 구현
- 이해관계자에게 기술적 의사결정과 진행 상황 전달

현재 프로젝트 적용:

- Unreal Engine 5 클라이언트와 ASP.NET 서버, 모바일 패널을 end-to-end로 연결했다.
- 모바일 패널의 버튼 또는 AI Guide의 structured command가 Unreal 캐릭터 감정, 애니메이션, 무대 분위기 변경으로 이어진다.
- `Happy`, `Aged`, `Classic`, `Wave`, `Bow`, `Clap`, spotlight 등의 command를 실시간으로 실행한다.
- AI가 자연어 입력을 `setEmotion`, `playAnimation`, `triggerStageEvent`로 변환하고, 서버가 안전한 command만 Unreal에 전달한다.
- LLM의 language understanding과 Unreal animation/state control을 연결한 초기 AI-driven character control 구조를 구현했다.
- 패키징된 Unreal exe와 로컬 서버/모바일 패널 접근 흐름을 검증했다.

현재 부족한 점:

- 실제 motion generation 모델은 아직 적용하지 않았다.
- MDM, MotionGPT, T2M-GPT, MoMask 등 text-to-motion 모델을 직접 실행하거나 생성 asset을 Unreal에 import한 검증은 없다.
- HumanML3D, AMASS 등 motion dataset을 직접 다루지 않았다.
- retargeting, IK, root motion, foot sliding 보정 같은 motion pipeline 경험은 아직 프로젝트에 명확히 드러나지 않는다.
- low-level graphics programming(OpenGL/Vulkan/WebGPU) 경험은 없다.
- computational geometry나 graphics/ML 논문 구현 경험도 현재 프로젝트에는 포함되어 있지 않다.
- 3D 전시물과 캐릭터 animation 완성도가 아직 포트폴리오 핵심으로 보기엔 부족하다.

3D Graphics/Motion 기준 현재 강점:

- Unreal 기반 인터랙티브 시스템과 AI command pipeline을 실제로 연결했다.
- AI가 감정/상황을 판단하고 캐릭터 animation/state를 바꾸는 구조는 공고의 “AI 시스템과 real-time animation 통합” 방향과 연결된다.
- 모바일 패널, 서버, Unreal의 네트워크 구조와 패키징 실행 흐름이 있어 제품형 데모로 설명 가능하다.

3D Graphics/Motion 보강 우선순위:

1. Unreal 레벨에 전시물 3~5개 추가
2. 전시물별 lighting/style/animation trigger를 명확히 연결
3. Mixamo 또는 Marketplace animation을 캐릭터에 retargeting한 과정을 기록
4. AI command가 캐릭터 gesture를 선택하는 demo scenario 제작
5. offline text-to-motion 모델로 wave/bow/point 같은 짧은 gesture를 생성해 Unreal import 테스트
6. 성공/실패를 포함한 motion generation 실험 로그 작성

### 3D Graphics/Motion 자격요건/우대사항 매핑

공고상 자격요건/우대사항 요약:

- PyTorch 등 딥러닝 프레임워크 활용
- Computer Graphics/Vision 기초 이해
- 3D modeling, rigging, texturing, retargeting, IK 이해
- motion generation 또는 animation 관련 연구/프로젝트 경험
- Transformers, Diffusion, GAN, VAE 등 ML architecture 기반 문제 해결
- multimodal AI 모델 개발 경험
- MLOps 또는 클라우드 학습/배포 경험
- computational geometry, low-level graphics programming
- 오픈소스 공개/기여
- AI 기반 제품/서비스 개발 및 출시 경험

현재 충족:

- Unreal 기반 구현: 충족
- AI 기반 제품형 데모: 부분 충족
- 자연어/AI와 캐릭터 animation command 연결: 부분 충족
- real-time interaction pipeline: 충족
- 엔지니어링 구조 설명 가능성: 충족

현재 미충족 또는 약함:

- PyTorch 기반 motion model 구현: 미충족
- motion generation 연구/프로젝트: 미충족
- retargeting/IK pipeline: 약함
- multimodal AI 모델: 미충족
- low-level graphics programming: 미충족
- computational geometry: 미충족
- graphics/ML 논문 구현: 미충족

지원서에서 표현할 때의 핵심 문장:

```text
Unreal Engine 전시 클라이언트와 ASP.NET/SignalR 기반 제어 서버, 모바일 패널, LLM Gateway를 연결하여 자연어 입력이 캐릭터 감정, 애니메이션, 무대 분위기 변경으로 이어지는 AI-driven interactive 3D demo를 구현했습니다.
```

### 현재 지원 전략 결론

현재 프로젝트는 `LLM Engineer` 지원에 가장 강하게 맞는다. 특히 OpenAI API, prompt engineering, RAG context, multi-turn memory, native streaming, structured output, command validation, AI Gateway 분리 구조는 공고의 담당업무와 직접적으로 연결된다.

`Software Engineer / 3D Graphics, Motion`도 Unreal 기반 구현과 AI-to-animation command pipeline 덕분에 연결점은 있다. 다만 motion generation, retargeting, PyTorch 기반 모델 실험이 아직 없어 핵심 자격요건에는 약하다. 3D/Motion 지원까지 강하게 가져가려면 전시물 추가와 animation/retargeting/motion generation 실험을 반드시 보강해야 한다.

따라서 현재 추천 우선순위는 다음과 같다.

1. 1순위 지원: LLM Engineer
2. 2순위 지원: Software Engineer / 3D Graphics, Motion
3. 보류: AI Sound/Voice Engineer

다음 작업은 전시물/knowledge를 먼저 채우고, 그 다음 embedding RAG와 평가셋을 추가하는 것이 가장 효율적이다.

## 2026-04-28 프로젝트 적용 방식 중심 정리

이 섹션은 기술 키워드 나열이 아니라, 현재 프로젝트에서 실제로 어떤 문제를 어떤 방식으로 풀었는지와 그 결과를 설명하기 위한 지원서/면접용 정리다.

### LLM Engineer 관점: 어떻게 적용했는가

#### 1. AI API key를 클라이언트와 로컬 실행 파일에서 분리

문제:

- Steam 또는 패키징된 exe 기준으로 OpenAI API key가 클라이언트 파일, Unreal 프로젝트, 로컬 서버 설정에 들어가면 유출 위험이 있다.
- 모바일 패널과 Unreal 클라이언트는 사용자가 직접 접근 가능한 영역이므로 비밀값을 넣으면 안 된다.

적용 방식:

- `ExhibitionServer`는 로컬 제어 서버 역할만 담당하게 두고, OpenAI 호출은 `Cloud-AI-Gateway/ExhibitionAiGateway`로 분리했다.
- 모바일 패널은 로컬 `ExhibitionServer`에만 요청한다.
- `ExhibitionServer`는 `AI_GATEWAY_CLIENT_KEY`로 Gateway에 인증된 요청을 보낸다.
- 실제 `OPENAI_API_KEY`는 Gateway 환경변수에만 존재하도록 설계했다.
- provider 구조를 `IAiChatProvider`로 추상화해서 OpenAI, Gemini, Groq, Local LLM으로 교체 가능한 구조를 만들었다.

결과/퍼포먼스:

- 클라이언트 배포물에 OpenAI API key가 포함되지 않는다.
- 로컬 서버가 Gateway 장애 시 rule/RAG fallback으로 응답할 수 있어 전체 체험이 바로 끊기지 않는다.
- provider 교체 비용이 낮아졌다. 새 provider는 named HttpClient와 `IAiChatProvider` 구현만 추가하면 된다.

#### 2. 자연어를 Unreal command로 바로 실행하지 않고 structured output과 whitelist를 거치게 함

문제:

- LLM이 자유 텍스트로 command를 만들면 Unreal에 위험하거나 지원하지 않는 명령이 전달될 수 있다.
- 사용자가 "움직여", "분위기 바꿔", "웃으면서 인사해"처럼 모호하게 말해도 실제 실행 가능한 command는 제한되어 있어야 한다.

적용 방식:

- Gateway prompt에서 LLM 출력 형식을 JSON object로 제한했다.
- 출력 schema는 `reply`와 `suggestedCommands`로 나눴다.
- command type은 `setEmotion`, `playAnimation`, `triggerStageEvent`만 허용했다.
- `ExhibitionServer`에서 다시 한 번 emotion, animation, stageEvent whitelist를 검증한다.
- 검증을 통과한 command만 `SetEmotionCommand`, `PlayAnimationCommand`, `TriggerStageEventCommand`, `SwitchStyleCommand`로 변환한다.
- Unreal로 전달되는 최종 command는 기존 command pipeline을 그대로 사용한다.

결과/퍼포먼스:

- LLM hallucination이 있어도 지원하지 않는 command는 실행되지 않는다.
- "Happy", "Aged", "Classic", "Wave", "Bow", "Clap", spotlight 같은 이미 검증된 command만 Unreal에 전달된다.
- LLM 응답과 Unreal 제어를 분리했기 때문에, 답변 텍스트가 실패해도 command validation 계층에서 안전하게 차단할 수 있다.

#### 3. RAG는 먼저 in-memory keyword search로 연결하고, embedding RAG로 확장 가능한 경계로 둠

문제:

- 전시물 설명은 LLM 일반 지식이 아니라 프로젝트 내부 knowledge를 기준으로 해야 한다.
- 아직 전시물이 확정되지 않은 상태에서 vector DB부터 도입하면 구조는 커지지만 검증할 데이터가 부족하다.

적용 방식:

- `IExhibitionKnowledgeStore` 인터페이스를 두고 전시물 knowledge 검색을 서버 application layer에 숨겼다.
- 현재는 JSON 기반 전시물 문서를 in-memory로 로드하고, message와 selectedArtifactId를 기준으로 검색한다.
- 검색 결과는 `AiRetrievedContextDto`로 변환되어 Gateway의 `AiChatRequest.RetrievedContext`에 포함된다.
- OpenAI prompt의 `Retrieved exhibition context` 섹션에 title, id, summary, tags를 넣는다.
- 향후 embedding RAG는 `IExhibitionKnowledgeStore` 구현만 교체하면 되도록 설계했다.

결과/퍼포먼스:

- 현재 단계에서도 LLM이 프로젝트 전시물 context를 받아 답변할 수 있다.
- RAG 고도화 전에 UI, 서버, Gateway, Unreal command 연결을 먼저 검증했다.
- 내일 전시물 3~5개가 추가되면 keyword search와 embedding search 결과를 비교할 수 있는 구조가 준비되어 있다.

#### 4. 멀티턴 memory는 DB가 아니라 세션 단위 최근 대화로 제한

문제:

- AI 가이드가 "방금 말한 작품", "그걸 다시 설명해줘" 같은 문맥을 이해하려면 대화 이력이 필요하다.
- 하지만 장기 기억을 먼저 만들면 개인정보, 저장 정책, vector memory 설계까지 범위가 커진다.

적용 방식:

- 모바일 패널이 `conversationId`를 localStorage에 생성한다.
- `/api/chat`, `/api/chat/stream` 요청에 `conversationId`를 포함한다.
- `ExhibitionServer`의 `ConversationMemoryStore`가 conversationId별 최근 5턴, 총 10개 message를 in-memory로 보관한다.
- Gateway 요청에는 `ConversationHistory` 배열을 포함한다.
- `OpenAiResponsesProvider.BuildInput()`에서 `Recent conversation history`를 user message와 retrieved context 앞에 넣는다.

결과/퍼포먼스:

- 서버 재시작 전까지 같은 브라우저 세션의 대화 문맥을 유지한다.
- 장기 DB 없이도 멀티턴 UX를 검증할 수 있다.
- context window 폭주를 막기 위해 최근 10개 turn과 message 길이 제한을 둔다.

#### 5. Streaming은 endpoint만 만든 것이 아니라 end-to-end native stream 경로로 정리

문제:

- LLM 응답은 수 초가 걸릴 수 있어 사용자가 "멈춘 것"처럼 느낄 수 있다.
- 특히 전시 가이드 UI에서는 응답이 즉시 나오기 시작하는 체감이 중요하다.

적용 방식:

- 모바일 패널은 `stream/json/auto` 모드를 선택할 수 있다.
- `OpenAiResponsesProvider`는 OpenAI Responses API에 `stream = true`로 요청한다.
- OpenAI SSE의 `response.output_text.delta`를 읽는다.
- LLM 출력이 JSON object이므로, `ReplyStreamExtractor`가 `"reply"` 필드 내부 문자열만 실시간 추출한다.
- `AiChatStreamService`는 provider stream을 list로 모으지 않고 즉시 `yield return`한다.
- `AiGatewayClient`도 Gateway SSE를 list로 모으지 않고 `Channel<AiChatStreamEvent>`로 읽는 즉시 local stream에 전달한다.
- command 실행은 delta가 아니라 complete event 이후에만 수행한다.

결과/퍼포먼스:

- OpenAI -> Gateway -> ExhibitionServer -> Mobile Panel까지 중간 buffering 없는 streaming 구조가 되었다.
- 텍스트는 먼저 표시하고, command는 최종 JSON이 완성된 뒤 검증/실행하므로 UX와 안전성을 동시에 확보했다.
- JSON 모드도 남겨 두어 fallback, 비교, 디버깅이 가능하다.

#### 6. 대화 로그를 evaluation/fine-tuning dataset 후보로 남김

문제:

- LLM 기능은 한 번 동작하는 것보다, 실패 사례를 수집하고 개선하는 루프가 중요하다.
- 지원서에서 "prompt engineering"을 말하려면 개선 근거가 되는 로그 구조가 필요하다.

적용 방식:

- `ConversationLogger`가 `Data/Logs/chat-log.jsonl`에 대화별 로그를 남긴다.
- 로그에는 user text, conversationId, selectedArtifactId, retrievedSources, reply, generatedCommands, executedCommandIds, success, latencyMs, promptVersion을 포함한다.
- JSON 응답과 streaming 응답 모두 complete 시점에 로그를 남긴다.

결과/퍼포먼스:

- 어떤 질문에서 어떤 전시물이 검색됐고, 어떤 command가 생성/실행됐는지 추적할 수 있다.
- 이후 평가셋을 만들 때 실제 사용자 질문과 실패 사례를 seed data로 사용할 수 있다.
- fine-tuning까지는 아직 아니지만, dataset 준비 단계로 설명 가능하다.

### 3D Graphics/Motion 관점: 어떻게 적용했는가

#### 1. Unreal을 단순 화면이 아니라 AI command의 실행 대상로 연결

문제:

- LLM 챗봇만 있으면 3D/Motion 공고와의 연결이 약하다.
- 사용자의 자연어가 실제 3D 캐릭터 상태 변화로 이어져야 interactive AI demo로 설명할 수 있다.

적용 방식:

- 모바일 패널, ASP.NET 서버, Unreal 클라이언트를 하나의 command pipeline으로 연결했다.
- AI Guide가 생성한 structured command를 서버가 Unreal command DTO로 변환한다.
- Unreal은 emotion, animation, stage event command를 받아 캐릭터와 무대 상태를 바꾼다.
- 버튼 제어와 AI 제어가 같은 command pipeline을 사용하므로, 수동 제어와 AI 제어의 결과를 비교할 수 있다.

결과/퍼포먼스:

- "웃으면서 인사해줘" 같은 자연어가 `setEmotion happy` + `playAnimation wave`로 변환되어 캐릭터 반응으로 이어진다.
- `Aged`, `Classic`, spotlight 같은 stage command도 AI가 제안할 수 있다.
- LLM이 직접 Unreal을 조작하는 것이 아니라 검증된 command만 통과하므로 안정성이 높다.

#### 2. 모바일 패널을 실시간 3D 제어 인터페이스로 사용

문제:

- Unreal 데모는 PC 앞에서만 조작하면 전시/팬 경험 관점에서 약하다.
- 노트북/모바일 환경에서 접근 가능한 컨트롤 패널이 있어야 설치형 데모로 설득력이 생긴다.

적용 방식:

- React/Vite 기반 모바일 패널을 ASP.NET `wwwroot`에 배포한다.
- QR 또는 노트북 IP를 통해 모바일 브라우저에서 접근한다.
- joystick, rotate, emotion, animation, stage command, AI Guide를 하나의 패널에서 제공한다.
- 서버는 SignalR/WebSocket command pipeline으로 Unreal과 통신한다.

결과/퍼포먼스:

- 패키징된 exe 실행 환경에서도 모바일 패널로 Unreal 캐릭터/무대 제어가 가능하다.
- AI Guide와 수동 조작을 같은 UI에서 사용할 수 있어 전시형 인터랙션에 가깝다.

#### 3. AI-driven animation selection의 초기 구조를 구현

문제:

- 아직 motion generation 모델은 없지만, AI가 어떤 motion을 선택할지 판단하는 계층은 먼저 필요하다.
- text-to-motion을 붙이더라도 최종적으로 Unreal에서 실행 가능한 animation intent로 변환해야 한다.

적용 방식:

- LLM output schema에 `playAnimation` command를 포함했다.
- animation whitelist는 `wave`, `bow`, `clap`, `explain`, `idle`로 제한했다.
- 서버는 LLM suggestion을 `PlayAnimationCommand`로 변환하고 Unreal에 전달한다.
- 이 구조는 향후 motion generation 결과물을 Unreal animation asset으로 추가했을 때 그대로 확장 가능하다.

결과/퍼포먼스:

- 현재는 생성형 motion이 아니라 준비된 animation asset 선택 방식이다.
- 그러나 "자연어/감정/상황 -> animation intent -> Unreal animation 실행" 경로는 이미 만들어져 있다.
- 향후 offline text-to-motion으로 만든 gesture asset을 whitelist에 추가하면 AI가 생성 motion을 선택하는 구조로 확장할 수 있다.

#### 4. 현재 3D/Motion에서 부족한 실제 적용 부분

현재 프로젝트는 3D/Motion 공고의 "Unreal 기반 AI interaction"에는 연결되지만, 다음 항목은 아직 직접 적용하지 못했다.

- text-to-motion 모델 실행
- motion diffusion 기반 animation 생성
- HumanML3D/AMASS 데이터 처리
- generated motion의 FBX/BVH export
- Unreal skeleton retargeting
- IK/root motion/foot sliding 보정
- low-level graphics programming
- computational geometry

따라서 3D/Motion 지원까지 강하게 가져가려면 다음 실험을 추가해야 한다.

1. Mixamo 또는 Marketplace animation을 캐릭터에 retargeting하고 과정을 기록한다.
2. offline text-to-motion 모델로 wave/point/bow 같은 짧은 gesture를 생성한다.
3. 생성 결과를 FBX/BVH로 변환하거나 가능한 포맷으로 Unreal에 import한다.
4. AI Guide의 `motionIntent` 또는 `playAnimation` command가 해당 generated motion을 선택하게 한다.
5. 성공한 motion과 실패한 motion을 비교해 retargeting 문제, foot sliding, skeleton mismatch를 문서화한다.

### 현재 프로젝트를 설명하는 방식

좋은 설명:

```text
이 프로젝트는 OpenAI API를 단순 호출하는 챗봇이 아니라, 전시 knowledge 검색 결과와 최근 대화 이력을 prompt에 구성하고, LLM의 structured output을 서버에서 whitelist 검증한 뒤 Unreal Engine 캐릭터의 감정, 애니메이션, 무대 상태로 변환하는 end-to-end AI interaction pipeline입니다. 또한 OpenAI native streaming을 Gateway와 로컬 서버를 거쳐 모바일 패널까지 전달해 응답 체감을 개선했고, 대화 로그를 JSONL로 저장해 평가셋과 fine-tuning 데이터 후보로 활용할 수 있게 했습니다.
```

피해야 할 설명:

```text
OpenAI API, RAG, streaming, multi-turn, Unreal을 사용했습니다.
```

이 표현은 기술 나열에 가깝다. 실제 지원서/면접에서는 "왜 분리했는지", "어디서 검증했는지", "어떤 데이터가 흘러가는지", "결과적으로 어떤 문제가 해결됐는지"를 중심으로 설명해야 한다.

## LLM Engineer 핵심 개념과 현재 프로젝트 연결

이 섹션은 공고에 등장하는 LLM fine-tuning, prompt engineering, inference, model serving, MLOps가 무엇을 의미하는지와 현재 프로젝트에서 어디까지 연결되는지를 정리한다.

### Prompt Engineering

개념:

- 모델 자체를 바꾸지 않고, 입력 prompt를 설계해서 원하는 형식과 행동을 안정적으로 끌어내는 작업이다.
- 역할, 출력 형식, 금지 규칙, 예외 처리, 응답 톤, command schema 등을 prompt에 명확히 넣는다.

현재 프로젝트 적용:

- `OpenAiResponsesProvider`에서 AI를 Unreal 전시 가이드 역할로 지정한다.
- 응답을 자유 텍스트가 아니라 JSON object로 제한한다.
- `reply`와 `suggestedCommands`를 분리한다.
- command type을 `setEmotion`, `playAnimation`, `triggerStageEvent`로 제한한다.
- movement, file, shell, network 같은 임의 실행 command를 만들지 말라고 명시한다.
- greeting, classic/aged/modern mood 같은 자주 쓰는 intent에 대한 command 선택 규칙을 넣었다.

결과:

- LLM 답변을 바로 사용자에게 보여주는 것뿐 아니라, Unreal에서 실행 가능한 command로 안정적으로 변환할 수 있다.
- prompt가 command safety의 1차 방어선 역할을 하고, 서버 whitelist가 2차 방어선 역할을 한다.

추가로 하면 좋은 것:

- prompt version을 명시적으로 관리한다.
- 동일 질문에 대한 기대 JSON을 evaluation set으로 만들어 prompt 변경 전후를 비교한다.
- 실패 사례를 `chat-log.jsonl`에서 뽑아 prompt rule을 개선한다.

### LLM Fine-tuning

개념:

- prompt로 매번 설명하는 대신, 모델이 특정 도메인/말투/출력 형식을 더 잘 따르도록 추가 학습시키는 작업이다.
- 예를 들어 "밝게 인사해줘"라는 입력에 대해 어떤 reply와 어떤 command JSON을 내야 하는지 예시 데이터를 많이 만들어 학습시킨다.

현재 프로젝트 적용:

- 실제 fine-tuning은 아직 하지 않았다.
- 대신 fine-tuning에 사용할 수 있는 후보 데이터 구조는 만들기 시작했다.
- `ConversationLogger`가 사용자 입력, 응답, retrieved source, generated command, executed command, latency, promptVersion을 JSONL로 저장한다.

결과:

- 지금은 fine-tuned model이 아니라 prompt + structured output + validation 기반이다.
- 하지만 실제 사용 로그를 모아 "질문 -> 기대 답변 -> 기대 command" 형태의 dataset으로 정제할 수 있다.

추가로 하면 좋은 것:

- 질문 20~50개 규모의 작은 evaluation dataset을 먼저 만든다.
- 좋은 응답과 나쁜 응답을 구분해 fine-tuning 후보 데이터를 만든다.
- fine-tuning 전에는 prompt/RAG 개선으로 해결 가능한 문제인지 먼저 판단한다.

### Inference

개념:

- 학습된 모델이 실제 요청을 받아 답변을 생성하는 실행 과정이다.
- 사용자가 질문을 보내고 모델이 token을 생성하는 순간이 inference다.
- inference 최적화는 응답 시간, 비용, token 사용량, 동시 처리, timeout, fallback을 관리하는 일이다.

현재 프로젝트 적용:

- 모바일 패널이 질문을 보내면 `ExhibitionServer -> ExhibitionAiGateway -> OpenAI Responses API` 경로로 inference가 수행된다.
- JSON 모드와 stream 모드를 분리했다.
- stream 모드에서는 OpenAI native streaming을 사용해 `response.output_text.delta`를 받아온다.
- Gateway와 로컬 서버 모두 중간 buffering 없이 SSE를 전달하도록 수정했다.
- timeout, rate limit, fallback 구조를 둬서 AI 호출 실패 시 로컬 rule/RAG 응답으로 떨어질 수 있게 했다.

결과:

- 사용자는 전체 응답이 끝날 때까지 빈 화면으로 기다리지 않고, reply가 생성되는 동안 텍스트를 먼저 볼 수 있다.
- command 실행은 complete event 이후에만 수행되므로, streaming UX와 command safety를 분리했다.

추가로 하면 좋은 것:

- token usage와 비용을 로그에 남긴다.
- provider/model별 latency를 비교한다.
- 짧은 인사/단순 command는 JSON 또는 local rule로 처리하고, 긴 설명만 LLM으로 보내는 routing을 추가한다.
- response cache를 적용할 수 있는 질문을 분리한다.

### Cloud Model Serving

개념:

- 모델 또는 AI 기능을 서버/API 형태로 운영해서 다른 클라이언트가 호출할 수 있게 만드는 것이다.
- 직접 LLM을 GPU 서버에 올리는 방식도 있고, OpenAI 같은 외부 API를 감싼 Gateway를 운영하는 방식도 있다.

현재 프로젝트 적용:

- 현재는 직접 모델을 서빙하지 않고, OpenAI API를 감싸는 `ExhibitionAiGateway` 방식이다.
- 로컬 `ExhibitionServer`는 Unreal 제어와 모바일 패널 serving을 담당하고, AI Gateway는 외부 AI API 호출과 provider 추상화를 담당한다.
- Steam 배포를 고려해 OpenAI key가 로컬 exe나 모바일 bundle에 들어가지 않도록 분리했다.
- `AI_GATEWAY_CLIENT_KEY`로 로컬 서버와 Gateway 사이의 호출을 제한한다.

결과:

- 배포 파일에 OpenAI API key가 포함되지 않는다.
- OpenAI, Gemini, Groq, Local LLM provider를 교체할 수 있는 구조가 생겼다.
- 클라우드에 Gateway를 올리면 같은 로컬 패키징 파일로도 AI 기능을 사용할 수 있다.

추가로 하면 좋은 것:

- 실제 Cloud AI Gateway 배포를 수행한다.
- Gateway에 request quota, per-client rate limit, usage logging을 추가한다.
- health check, structured logging, deployment profile을 정리한다.
- 나중에 Gemma/Llama를 직접 서빙한다면 vLLM, Ollama, TGI, FastAPI 같은 serving stack을 비교한다.

### MLOps

개념:

- ML/LLM 기능을 개발, 배포, 모니터링, 평가, 개선하는 운영 체계다.
- 일반 DevOps에 데이터 수집, 데이터 정제, 모델 평가, 모델 버전 관리, 재학습, 추론 품질 모니터링이 추가된 개념이다.

현재 프로젝트 적용:

- 아직 완전한 MLOps는 아니다.
- 하지만 MLOps로 확장할 수 있는 기반은 일부 있다.
- 대화 로그 JSONL 저장
- promptVersion 기록
- provider/model/latency 기록 구조
- command 생성과 실행 결과 분리
- AI Gateway와 local server 분리
- fallback 경로

결과:

- 어떤 질문에서 어떤 context가 들어갔고, 어떤 command가 생성/실행됐는지 추적할 수 있다.
- prompt 또는 model을 바꿨을 때 비교할 수 있는 최소한의 로그 기반이 생겼다.

추가로 하면 좋은 것:

- evaluation dataset을 만든다.
- prompt/model version별 success rate를 계산한다.
- retrieved source score, command validation failure reason을 로그에 남긴다.
- Gateway에 metrics endpoint를 추가한다.
- 배포 후 latency/error/rate-limit 현황을 모니터링한다.

### 현재 프로젝트를 LLM Engineer 개념으로 요약

```text
현재 프로젝트는 fine-tuned model을 직접 학습한 단계는 아니지만, prompt engineering, RAG context injection, multi-turn memory, native streaming inference, AI Gateway 기반 serving, command validation, conversation logging을 포함한 LLM application pipeline을 구현한 상태다.
```

가장 강하게 말할 수 있는 부분:

- prompt engineering
- structured output
- native streaming inference
- AI Gateway serving architecture
- multi-turn memory
- command safety
- logging 기반 evaluation/fine-tuning dataset 준비

아직 약한 부분:

- 실제 LLM fine-tuning
- embedding/vector DB 기반 RAG
- 직접 모델 서빙
- MLOps 자동화
- 비용/token 최적화 지표

## 2026-05-02 현재 구현 기준 Role Fit 재정리

이 섹션은 계획이 아니라 현재 프로젝트에 실제로 들어간 구현을 기준으로 정리한다. Text-to-motion은 별도 문서 `NextProject/TEXT-TO-MOTION-AUTOMATION-PLAN.md`로 분리하고, 여기서는 현재 Blue Garage 지원 관점에서 충분한 점과 부족한 점을 구분한다.

### 1. 현재 계획 대비 완료된 것

초기 `PLAN.MD`와 `MVP-READINESS-CHECKLIST.md`에서 목표로 잡았던 핵심 흐름은 대부분 구현됐다.

완료된 핵심 흐름:

- Unreal 실행 클라이언트
- ASP.NET Core 로컬 서버
- 모바일 패널 정적 빌드 및 서버 서빙
- 노트북 IP 기반 모바일 패널 접속
- 모바일 패널 -> 서버 -> Unreal 명령 전달
- 캐릭터 emotion / animation / stage style command 전달
- QR 접근 구조
- OpenAI API를 직접 로컬 exe에 넣지 않는 AI Gateway 구조
- AI chat JSON 응답
- native streaming 응답
- multi-turn conversation memory
- 전시물 JSON knowledge base
- OpenAI embedding 기반 in-memory hybrid RAG
- 전시물 catalog intent
- 채팅 기반 전시장 분위기 변경 command
- LLM suggested command에 대한 서버 whitelist 검증

즉, 현재 프로젝트는 단순한 Unreal 화면이 아니라 `모바일 조작 + 로컬 서버 + AI Gateway + RAG + Unreal 반응`이 end-to-end로 연결된 상태다.

### 2. LLM Engineer 기준 충분한 점

현재 가장 강한 지원 포지션은 여전히 `LLM Engineer`다.

충분한 이유:

- LLM API를 단순 호출하는 수준이 아니라, API key 노출을 피하기 위해 `ExhibitionAiGateway`를 별도 프로젝트로 분리했다.
- Steam/패키징 배포 기준으로 로컬 서버와 클라우드 AI Gateway 역할을 분리했다.
- OpenAI provider만 고정하지 않고 Gemini/Groq/Local LLM provider로 확장 가능한 provider abstraction을 만들었다.
- LLM 출력은 자유 텍스트가 아니라 `reply + suggestedCommands` 구조의 JSON으로 제한했다.
- `ExhibitionServer`에서 command whitelist를 통과한 명령만 Unreal에 전달한다.
- streaming 응답은 단순 fake stream이 아니라 OpenAI native streaming SSE를 받아 Gateway와 로컬 서버를 거쳐 모바일 패널까지 흘리는 구조로 확장했다.
- multi-turn memory를 conversation id 기반으로 관리해 이전 대화 맥락을 다음 요청에 넣는다.
- RAG는 단순 keyword search에서 OpenAI embedding + cosine similarity 기반 hybrid search로 확장했다.
- AI가 실패하거나 Gateway가 꺼져도 rule/RAG fallback으로 최소 답변과 명령을 유지한다.
- conversation log를 남겨 추후 evaluation dataset, prompt 개선, fine-tuning 데이터 후보로 사용할 수 있다.

구체적 적용 예시:

```text
사용자: "트리케라톱스 설명해줘"

1. ExhibitionServer가 전시물 knowledge base에서 관련 문서를 검색
2. embedding RAG가 Triceratops document를 context로 선택
3. ExhibitionAiGateway가 context와 conversation history를 OpenAI Responses API에 전달
4. LLM이 한국어 답변과 suggestedCommands를 JSON으로 생성
5. ExhibitionServer가 suggestedCommands를 whitelist로 검증
6. playAnimation explain 같은 안전한 Unreal command만 실행
7. 모바일 패널에는 streaming으로 답변 표시
8. Unreal 캐릭터는 explain animation으로 반응
```

다른 예시:

```text
사용자: "클래식 분위기로 바꿔줘"

1. 서버가 scene-control intent를 감지
2. LLM이 command를 빠뜨려도 deterministic command 보강
3. switchStyle styleIndex=0 command 생성
4. Unreal stage style 변경
```

이 부분은 `prompt engineering`, `RAG`, `inference`, `cloud serving`, `structured output`, `command safety`를 한 프로젝트 안에서 보여준다.

### 3. LLM Engineer 기준 아직 부족한 점

아직 약한 부분도 명확하다.

- 실제 LLM fine-tuning은 하지 않았다.
- OpenAI embedding을 쓰고 있지만 외부 Vector DB는 아직 붙이지 않았다.
- RAG evaluation dataset과 정량 지표가 없다.
- hallucination rate, retrieval hit rate, command success rate 같은 측정 지표가 없다.
- prompt/model version별 A/B test 구조는 아직 없다.
- AI Gateway를 실제 클라우드에 배포한 상태는 아니다.
- MLOps 관점의 monitoring, alerting, dashboard가 없다.
- token/cost 최적화 지표가 없다.

다만 지금 단계에서 Vector DB를 바로 붙이지 않은 것은 약점이라기보다 합리적인 선택이다. 현재 전시물은 3개 수준이라 외부 DB 운영보다 `IExhibitionKnowledgeStore`, `IKnowledgeVectorSearch` 인터페이스를 먼저 분리하고 in-memory vector search로 시작하는 편이 더 실무적이다. 전시물이 수십~수백 개로 늘어나면 Qdrant, pgvector, Azure AI Search 같은 Vector DB로 교체할 수 있다.

### 4. 3D Graphics / Motion 기준 충분한 점

`Software Engineer / 3D Graphics, Motion` 관점에서는 motion generation 자체보다, Unreal 기반 실시간 제어와 콘텐츠 파이프라인 쪽이 현재 강점이다.

충분한 점:

- Unreal Engine 5 기반의 전시 공간을 구성했다.
- 모바일 패널 입력을 Unreal 캐릭터 이동/회전/감정/애니메이션/무대 연출로 연결했다.
- `SetEmotion`, `PlayAnimation`, `SwitchStyle`, `TriggerStageEvent` 같은 command 계약을 분리했다.
- 서버에서 들어오는 명령을 Unreal이 실시간으로 반응하는 구조를 만들었다.
- Smithsonian 3D 전시물을 실제 UE 레벨에 배치했다.
- 전시물 metadata를 RAG context와 Unreal 전시 asset 양쪽에 연결했다.
- 채팅에서 전시장 분위기를 바꾸는 scene-control flow를 만들었다.

구체적 적용 예시:

```text
사용자: "Bell X-1 소개해줘"

1. RAG가 Bell X-1 metadata를 검색
2. LLM이 설명 답변 생성
3. 서버가 explain animation command를 보강
4. Unreal 캐릭터가 설명 제스처를 수행
5. 사용자는 3D 전시물 앞에서 AI 가이드의 설명과 캐릭터 반응을 함께 본다
```

이건 단순히 3D asset을 배치한 것이 아니라, 전시물 metadata, AI 응답, 캐릭터 animation, stage command를 하나의 관람 흐름으로 묶은 것이다.

### 5. 3D Graphics / Motion 기준 부족한 점

현재 가장 큰 약점은 공고의 `motion generation` 키워드와 직접 연결되는 실험이 아직 없다는 점이다.

부족한 점:

- MDM, MotionGPT, T2M-GPT 같은 text-to-motion 모델을 직접 돌린 경험은 아직 없다.
- AMASS, HumanML3D 같은 motion dataset 전처리 경험은 아직 없다.
- 생성된 joint sequence를 BVH/FBX로 변환한 경험은 없다.
- 생성된 motion을 UE skeleton으로 retarget한 결과물이 아직 없다.
- IK Rig / IK Retargeter를 활용한 generated animation 검증 영상이 없다.
- foot sliding, root motion, skeleton mismatch 같은 motion 품질 문제를 분석한 기록이 없다.

따라서 3D/Motion 지원 적합도를 더 높이려면 별도 후속 작업으로 text-to-motion 실험을 1개라도 완성하는 것이 좋다.

추천 후속 작업은 `NextProject/TEXT-TO-MOTION-AUTOMATION-PLAN.md`에 분리했다.

### 6. Text-to-Motion은 현재 어디에 위치하는가

Text-to-motion은 현재 MVP 필수 기능이 아니라, role fit 보강용 후속 실험이다.

지금 프로젝트에서 적합한 방식:

```text
Text-to-motion 도구로 wave/bow/explain 계열 짧은 gesture 생성
-> FBX export
-> Unreal import
-> IK Retargeter로 현재 캐릭터에 맞춤
-> animation key 등록
-> LLM이 motionIntent를 선택
-> 서버 whitelist 검증 후 playAnimation 실행
```

이렇게 구현하면 LLM Engineer와 3D/Motion을 동시에 연결할 수 있다.

- LLM Engineer: 자연어를 안전한 `motionIntent` / `playAnimation` command로 변환
- 3D/Motion: AI-generated motion asset을 Unreal animation pipeline에 적용

단, 아래 표현은 피해야 한다.

```text
실시간 text-to-motion generation을 구현했습니다.
```

현재 단계에서 더 정확한 표현은 다음이다.

```text
LLM이 사용자 자연어를 motion intent로 해석하고,
text-to-motion 도구로 생성한 animation asset을 Unreal 캐릭터의 안전한 animation command로 재생하는 구조를 설계했습니다.
```

### 7. 현재 프로젝트에서 role fit을 위해 실제 적용한 기법과 효과

#### 7.1 AI Gateway 분리

적용 방식:

- `Cloud-AI-Gateway/ExhibitionAiGateway`를 별도 ASP.NET 프로젝트로 생성했다.
- 로컬 `ExhibitionServer`는 OpenAI API를 직접 호출하지 않고 Gateway를 호출한다.
- Gateway에는 provider abstraction, client key 인증, rate limit 구조를 둔다.

효과:

- Steam/패키징 파일에 OpenAI API key가 들어가는 문제를 피할 수 있다.
- 추후 OpenAI에서 Gemini/Groq/Local LLM으로 provider를 교체할 수 있다.
- 로컬 실시간 제어 서버와 클라우드 AI inference 서버의 책임이 분리된다.

#### 7.2 Structured output + command whitelist

적용 방식:

- LLM 응답을 `reply`와 `suggestedCommands`로 구조화했다.
- 허용 command는 `setEmotion`, `playAnimation`, `triggerStageEvent`로 제한했다.
- 서버에서 허용 emotion/animation/stage event만 Unreal command로 변환한다.

효과:

- LLM이 임의의 명령이나 위험한 동작을 직접 실행하지 못한다.
- AI 답변과 Unreal 실행 명령을 분리해 디버깅할 수 있다.
- "밝게 인사해줘" 같은 자연어를 `happy + wave` 형태로 연결할 수 있다.

#### 7.3 Native streaming

적용 방식:

- OpenAI Responses API의 streaming SSE를 Gateway에서 받아 `AiChatStreamEvent`로 변환했다.
- 로컬 서버와 모바일 패널까지 delta/complete event 구조를 유지한다.
- complete event 이후 command를 검증하고 Unreal에 전달한다.

효과:

- 사용자는 전체 답변이 끝날 때까지 빈 화면으로 기다리지 않는다.
- LLM 답변 텍스트는 먼저 표시되고, 최종 JSON complete 후 안전한 command가 실행된다.
- 포트폴리오에서 `inference UX 최적화`를 설명할 수 있다.

#### 7.4 Multi-turn memory

적용 방식:

- 모바일 패널이 conversation id를 유지한다.
- 서버가 최근 대화 turn을 memory store에 저장한다.
- Gateway 요청에 recent conversation history를 포함한다.

효과:

- "그 전시물 더 자세히 설명해줘" 같은 후속 질문을 처리할 기반이 생겼다.
- 단발성 Q&A가 아니라 guide persona와 대화하는 구조로 확장 가능하다.

#### 7.5 Embedding RAG

적용 방식:

- 전시물 정보를 JSON knowledge document로 분리했다.
- keyword score와 OpenAI embedding cosine similarity를 결합한 hybrid search를 구현했다.
- 외부 Vector DB 대신 in-memory vector search로 시작하고, 인터페이스를 분리했다.

효과:

- "비행기", "공룡", "청동기"처럼 사용자가 전시물 정확한 이름을 몰라도 관련 context를 찾을 수 있다.
- 전시물이 적은 현재 단계에서는 운영 복잡도를 낮추고, 추후 Vector DB로 확장할 여지를 남겼다.

#### 7.6 Catalog / scene-control intent

적용 방식:

- "전시장에 전시물 종류 뭐야?" 같은 목록 질문은 similarity search가 아니라 catalog intent로 처리한다.
- "클래식 분위기로 바꿔줘" 같은 요청은 scene-control intent로 처리한다.
- LLM이 command를 누락해도 서버가 deterministic command를 보강한다.

효과:

- 일반 질문에서 엉뚱한 단일 전시물만 선택되는 문제를 줄인다.
- 전시장 분위기 변경은 AI 답변 품질과 별개로 안정적으로 Unreal command가 생성된다.
- 실제 시연에서 실패 가능성이 낮아진다.

### 8. 현재 최종 판단

현재 프로젝트는 `LLM Engineer` 기준으로는 지원 포트폴리오의 핵심 근거가 충분히 생겼다. 단순 API 호출이 아니라 Gateway, streaming, RAG, structured command, command safety, multi-turn, fallback까지 구현했기 때문이다.

`3D Graphics / Motion` 기준으로도 Unreal 실시간 제어와 전시형 인터랙션은 충분히 연결점이 있다. 다만 공고의 motion generation 키워드까지 강하게 맞추려면 text-to-motion asset을 최소 1~3개 생성해 UE에 retarget하고, chat command로 재생하는 후속 실험이 필요하다.

따라서 현재 우선순위는 다음과 같다.

1. 현재 AI 전시 가이드 시연 안정화
2. 전시물 metadata와 RAG 답변 품질 보강
3. emotion/animation command mapping 검증
4. 패키징 산출물 기준 재검증
5. 시간이 남으면 text-to-motion 오프라인 생성 asset 3개를 UE에 연결
---

## 9. 2026-05-03 기준 role fit 최신 업데이트

현재 프로젝트는 `LLM Engineer` 포지션 기준으로 설명할 수 있는 실제 구현 근거가 더 강해졌다. 핵심은 “AI API를 호출했다”가 아니라, Steam 배포를 고려해 로컬 실행 환경과 클라우드 AI inference 환경을 분리하고, 전시물 RAG와 Unreal command 실행을 안전하게 연결했다는 점이다.

### 9.1 LLM Engineer 관점에서 실제 적용한 방식

#### Cloud AI Gateway 분리

적용 방식:

- `Cloud-AI-Gateway/ExhibitionAiGateway`를 로컬 전시 서버와 별도 ASP.NET 프로젝트로 분리했다.
- Steam/Windows 패키지에 포함되는 `ExhibitionServer`는 OpenAI API key를 직접 갖지 않는다.
- 로컬 서버는 `AiGateway:BaseUrl`과 `AiGateway:ClientKey`만 가지고 Azure App Service의 Gateway를 호출한다.
- OpenAI API key, embedding model, LLM model 설정은 Azure App Service 환경 변수에서 관리한다.

효과:

- 다른 PC에서 exe를 실행해도 OpenAI key 없이 AI 답변이 가능하다.
- 클라이언트 패키지 탈취나 Steam depot 분석으로 OpenAI API key가 노출되는 문제를 피한다.
- 로컬 실시간 제어 서버와 클라우드 inference 서버의 책임이 분리된다.

포트폴리오에서 설명할 표현:

```text
Steam 배포 환경에서 API key가 클라이언트 패키지에 포함되지 않도록, 로컬 ExhibitionServer와 Cloud AI Gateway를 분리했습니다. 로컬 서버는 전시장 상태와 Unreal command dispatch를 담당하고, Azure App Service에 배포한 Gateway가 OpenAI inference와 embedding rerank를 담당하도록 구성했습니다.
```

#### RAG + embedding rerank

적용 방식:

- 전시물 정보는 JSON knowledge document로 관리한다.
- 로컬 `ExhibitionServer`는 전시물 후보 context를 선택한다.
- OpenAI embedding을 직접 호출하지 않고, Gateway 쪽에서 embedding rerank를 수행한다.
- 현재 전시물 수가 적기 때문에 외부 Vector DB는 붙이지 않고, 인터페이스 분리와 in-memory 기반 확장 구조를 우선했다.

효과:

- Steam 패키지에는 embedding API key도 포함되지 않는다.
- “공룡”, “비행기”, “청동기”처럼 정확한 전시물 이름이 아닌 질문도 관련 전시물 context로 연결할 수 있다.
- 전시물이 많아지면 Qdrant, pgvector, Azure AI Search 같은 Vector DB로 교체할 수 있는 구조적 여지를 남겼다.

포트폴리오에서 설명할 표현:

```text
전시물 metadata를 JSON knowledge document로 분리하고, 로컬 서버에서 후보 context를 만든 뒤 Cloud Gateway에서 OpenAI embedding 기반 rerank를 수행했습니다. 현재 데이터 규모에서는 외부 Vector DB 대신 in-memory 구조를 사용했지만, 검색 인터페이스를 분리해 추후 Qdrant/pgvector/Azure AI Search로 확장 가능하게 설계했습니다.
```

#### Streaming + multi-turn

적용 방식:

- Mobile Panel이 chat request를 보내면 로컬 서버가 Gateway stream endpoint를 호출한다.
- Gateway는 OpenAI native streaming을 받아 `delta`, `complete`, `error` 형태의 stream event로 변환한다.
- 로컬 서버는 이벤트를 다시 모바일 패널로 전달한다.
- conversation id 기반으로 이전 대화 일부를 다음 요청에 포함한다.

효과:

- 사용자가 답변 완료까지 빈 화면으로 기다리지 않고, 답변이 생성되는 즉시 볼 수 있다.
- “그 전시물 더 자세히 설명해줘” 같은 후속 질문을 처리할 기반이 생겼다.
- LLM inference UX 최적화, 대화 상태 관리, 서버 간 streaming relay 경험을 설명할 수 있다.

포트폴리오에서 설명할 표현:

```text
OpenAI native streaming을 Cloud Gateway에서 수신한 뒤, 로컬 ExhibitionServer를 거쳐 Mobile Panel까지 delta event로 전달했습니다. 최종 complete event 이후에는 suggested command를 검증해 Unreal에 전달하므로, 사용자는 빠른 응답 UX를 얻고 시스템은 안전한 command 실행 흐름을 유지합니다.
```

#### Structured output + command safety

적용 방식:

- LLM 응답은 `reply`와 `suggestedCommands`로 구조화한다.
- Unreal로 전달 가능한 명령은 whitelist로 제한한다.
- `setEmotion`, `playAnimation`, `triggerStageEvent` 같은 안전한 command만 통과시킨다.
- 설명 요청에는 `explain`, 인사에는 `wave`, 분위기 변경에는 `switchStyle`/stage command 계열로 연결한다.

효과:

- LLM이 생성한 임의 텍스트가 곧바로 Unreal 실행 명령이 되지 않는다.
- AI 답변과 실제 게임/전시 상태 변경 사이에 검증 계층이 생긴다.
- 실무적으로 중요한 “LLM output validation”과 “tool/command safety”를 보여줄 수 있다.

### 9.2 3D Graphics / Motion 관점에서 실제 적용한 방식

현재 프로젝트는 motion generation 모델을 직접 돌린 상태는 아니다. 대신 `모바일/자연어 입력 -> 서버 command -> Unreal character/stage reaction` 파이프라인이 강점이다.

적용 방식:

- Unreal Engine 5 전시장에 Smithsonian 3D 전시물을 배치했다.
- 모바일 패널 입력을 캐릭터 이동/회전, 감정, 애니메이션, 전시장 분위기 변경으로 연결했다.
- AI 답변 결과가 `playAnimation explain`, `setEmotion happy`, `triggerStageEvent` 같은 command로 변환되어 UE에 전달된다.
- QR로 여러 패널이 접속해도 현재 MVP는 single-control mode로 active character 1개만 유지한다.

효과:

- 단순 3D scene이 아니라 모바일 입력, AI guide, Unreal character reaction이 연결된 실시간 전시 시스템이 된다.
- 시연 중 QR 재접속이나 여러 패널 접속으로 캐릭터가 계속 늘어나는 문제를 막는다.
- 추후 Party Mode에서는 `playerId -> character` 매핑으로 4인 미니게임 구조까지 확장할 수 있다.

포트폴리오에서 설명할 표현:

```text
현재 MVP는 single-control mode로 설계해 여러 모바일 패널이 접속하더라도 하나의 active character만 유지합니다. 이후 Party Mode에서는 connectionId/playerId 기반 character mapping으로 확장해, 한 맵에 4명의 사용자가 접속해 각자 캐릭터를 조작하는 미니게임 구조로 발전시킬 수 있습니다.
```

### 9.3 아직 부족한 점

LLM Engineer 기준 부족한 점:

- 실제 fine-tuning은 아직 수행하지 않았다.
- RAG evaluation dataset, retrieval hit rate, hallucination rate 같은 정량 평가가 없다.
- token usage, latency, error rate, cost dashboard가 없다.
- Gemini/Groq/Local LLM provider는 확장 구조만 있고 실 provider 구현은 아직 없다.
- Azure 배포는 되었지만 CI/CD, staging slot, monitoring, alerting은 아직 부족하다.

3D Graphics / Motion 기준 부족한 점:

- MDM, MotionGPT, T2M-GPT 같은 text-to-motion 모델을 직접 실행한 결과물은 없다.
- 생성 motion을 FBX/BVH로 변환하고 UE skeleton에 retarget한 검증 결과가 없다.
- IK Rig / IK Retargeter 기반 generated animation 품질 검증 영상이 없다.
- 현재 애니메이션은 생성형 motion보다는 기존 animation command 실행에 가깝다.

### 9.4 현재 다음 단계 판단

지금 당장 추가 기능을 더 넣기보다, `Build/Windows` 패키지를 Steam 배포 가능한 형태로 검증하는 것이 우선이다.

우선순위:

1. UE 재패키징으로 active character 1개 유지 소스 반영
2. 다른 PC에서 packaged exe 단독 실행 확인
3. 모바일 QR 접속 확인
4. Azure Gateway 경유 AI 답변 확인
5. Steamworks depot 업로드 및 설치 테스트
6. 시간이 남으면 text-to-motion offline asset 1~3개를 UE에 연결

현재 결론:

```text
LLM Engineer 기준으로는 AI Gateway, streaming, multi-turn, RAG, embedding rerank, command safety, cloud deployment까지 연결되어 충분히 강한 포트폴리오 근거가 생겼다.

3D Graphics / Motion 기준으로는 Unreal 실시간 전시와 command-driven character reaction은 강하지만, motion generation 자체는 후속 보강 과제로 남아 있다.
```


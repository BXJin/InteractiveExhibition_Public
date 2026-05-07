# MVP 준비 상태 체크리스트

작성일: 2026-04-26

기준 문서:

- `Docs/Plan/PLAN.MD`
- `Docs/Plan/CHATBOT-MVP-PLAN.md`
- `Docs/Plan/BLUE-GARAGE-ROLE-FIT-AND-AI-GUIDE-PLAN.md`

## 0. 요약

현재 프로젝트는 기존 체크리스트 작성 시점보다 많이 진전됐다.

특히 아래는 이미 MVP 핵심에 가깝게 확인됐다.

- 패키징된 Unreal 실행물 존재
- ASP.NET 서버 publish 산출물 존재
- 서버 self-contained 실행 방향
- 모바일 패널 build 결과가 서버 `wwwroot`에 반영됨
- 노트북 IP로 모바일 패널 접속 확인
- Happy, Aged, Classic 같은 반응 확인
- ASP.NET에서 모바일 패널 URL/QR 생성

다만 아직 “최종 제출용 MVP 완료”로 보기에는 몇 가지가 남아 있다.

- 패키징 서버 경로 최종 정합성
- 원하는 Unreal 시작 레벨 고정
- 데모 시나리오/영상
- 운영/시연 모드 분리
- 챗봇/RAG 확장 구현

현재 MVP 준비도는 대략 `80~85%` 수준으로 본다.

## 1. 현재 완료된 작업

| 항목 | 상태 | 근거 |
|---|---|---|
| Unreal 프로젝트 | 완료 | `Client-Unreal/ExhibitionClient` |
| ASP.NET 서버 | 완료 | `Server-AspNet/ExhibitionServer` |
| Shared DTO 구조 | 완료 | `Shared/Exhibition.Shared` |
| React 모바일 패널 | 완료 | `Mobile-Panel` |
| 모바일 패널 build | 완료 | `Mobile-Panel/dist`, 서버 `wwwroot` 반영 |
| 서버 정적 파일 제공 | 완료 | `Server-AspNet/ExhibitionServer/wwwroot/index.html` 존재 |
| 서버 publish 산출물 | 완료 | `Build/Windows/ExhibitionServer/ExhibitionServer.exe` 존재 |
| 서버 자동 실행 구조 | 부분 완료 | Unreal launcher 코드 존재, 경로 정합성 재검증 필요 |
| 모바일 패널 접속 | 완료 | 노트북 IP 접속 확인 |
| URL 동적 생성 | 완료 | `PanelAccessController`, `PanelAccessService` |
| QR 생성 | 완료 | `/api/panel/qr.svg`, `/api/panel/qr.png` |
| Happy 반응 | 확인됨 | 사용자 실기기 확인 |
| Aged/Classic 맵/스타일 변화 | 확인됨 | 사용자 실기기 확인 |
| 이동/회전 입력 | 구현됨 | 모바일 패널 조이스틱/터치 입력 경로 |
| 패키징 실행 | 부분 완료 | 실행 확인, 원하는 레벨 설정은 재확인 필요 |

## 2. PLAN.MD 대비 달라진 점

### 2.1 계획보다 더 진행된 부분

초기 PLAN.MD는 모바일 패널과 Unreal 연동 중심이었다. 현재는 그보다 더 나아가 배포/현장 접속 흐름까지 일부 구현됐다.

추가된 부분:

- Unreal exe 실행 시 서버 자동 실행을 목표로 한 launcher 구조
- ASP.NET self-contained publish
- 모바일 패널 정적 파일을 ASP.NET `wwwroot`에서 제공
- 노트북 IP 기반 동적 URL 생성
- QR SVG/PNG 생성 API
- 패키징 폴더에 서버 포함
- Blue Garage 지원 분석 문서
- AI 전시 가이드 챗봇 계획 문서

### 2.2 계획과 다른 부분

| 계획 | 현재 | 판단 |
|---|---|---|
| 모바일 패널은 dev server 또는 웹 패널 | 현재는 서버 `wwwroot` 정적 배포도 사용 | 더 나은 방향 |
| QR은 미정 | 서버에서 QR API 구현 | 계획보다 진전 |
| 서버 실행은 별도 실행 가능성 | Unreal에서 자동 실행 구조 | 계획보다 진전 |
| 시작 레벨은 시연용 레벨이어야 함 | 패키징에서 원하지 않은 레벨 이슈 발생 | 설정 재확인 필요 |
| 텍스트 입력/AI 응답은 향후 확장 | 챗봇 문서화 완료, 구현은 아직 | 다음 단계 |
| 운영/시연 모드 분리 | 아직 명확하지 않음 | 부족 |

### 2.3 현재 프로젝트에 새로 추가된 가치

초기 계획에는 없었지만 현재 포트폴리오 설명에 도움이 되는 가치:

- “노트북 단독 시연”에 가까운 실행 구조
- 모바일 접속 URL/QR 자동 제공
- 서버와 패널을 함께 배포하는 구조
- 지원 직무별 분석 문서
- LLM/RAG 확장 계획

이 부분은 단순 Unreal 프로젝트가 아니라 “전시 현장 운영까지 고려한 인터랙티브 시스템”으로 설명할 수 있게 만든다.

## 3. 부족한 부분

### 3.1 즉시 해결해야 할 항목

| 우선순위 | 항목 | 이유 |
|---:|---|---|
| 1 | 패키징 서버 경로 정합성 | exe 실행 시 서버가 항상 같은 위치에서 실행되어야 함 |
| 2 | 시작 레벨 설정 | 원하지 않는 레벨로 패키징되면 시연 실패 |
| 3 | 최종 패키징 재검증 | Unreal + 서버 + 패널 + QR이 한 번에 되는지 확인 필요 |
| 4 | 데모 시나리오 작성 | 기능이 있어도 보여주는 순서가 없으면 약함 |
| 5 | 모바일 패널 크기 차이 점검 | 노트북/개발 PC 접속 시 조이스틱 크기 차이 발생 |

### 3.2 포트폴리오 품질을 위해 필요한 항목

- 1~2분 시연 영상
- 구조도
- 실행 흐름 다이어그램
- 상태 전이도
- 핵심 명령 목록
- 패키징/실행 가이드
- 트러블슈팅 문서

### 3.3 기능적으로 아직 부족한 항목

- 운영자 모드/관람객 모드 분리
- command cooldown
- 중복 입력 방지
- 명령 우선순위
- 마지막 명령/지연시간 HUD
- 전시물 단위 상호작용
- 챗봇 UI
- LLM API
- RAG/vector search
- conversation log

## 4. 체크리스트

### 4.1 실행/배포

| 체크 | 항목 | 상태 |
|---|---|---|
| [x] | Unreal 패키징 산출물 생성 | 완료 |
| [x] | ASP.NET 서버 publish | 완료 |
| [x] | 서버 exe 산출물 확인 | 완료 |
| [x] | 모바일 패널 build | 완료 |
| [x] | 서버 `wwwroot`에 패널 반영 | 완료 |
| [~] | Unreal exe 실행 시 서버 자동 실행 | 경로 재검증 필요 |
| [~] | 패키징 폴더 내 서버 위치 고정 | 재검증 필요 |
| [ ] | 최종 노트북 단독 재현 테스트 문서화 | 필요 |

### 4.2 모바일 접속

| 체크 | 항목 | 상태 |
|---|---|---|
| [x] | 노트북 IP로 모바일 패널 접속 | 완료 |
| [x] | 서버가 로컬 IP 감지 | 완료 |
| [x] | `/api/panel/url` 제공 | 완료 |
| [x] | `/api/panel/qr.svg` 제공 | 완료 |
| [x] | `/api/panel/qr.png` 제공 | 완료 |
| [~] | Unreal HUD에서 QR 표시 | 구현 경로 존재, 최종 시각 확인 필요 |
| [~] | 모바일 패널 크기 일관성 | 점검 필요 |

### 4.3 Unreal 반응

| 체크 | 항목 | 상태 |
|---|---|---|
| [x] | Happy 반응 | 확인됨 |
| [x] | Aged/Classic 변화 | 확인됨 |
| [x] | 모바일 이동 입력 | 구현됨 |
| [x] | 모바일 회전 입력 | 구현됨 |
| [~] | Wave/인사 애니메이션 | 최종 시연 확인 필요 |
| [~] | 조명/카메라/사운드 연출 | 실제 레벨 기준 확인 필요 |
| [~] | 원하는 시작 레벨 | 설정 필요 |

### 4.4 서버/명령 구조

| 체크 | 항목 | 상태 |
|---|---|---|
| [x] | Shared command DTO | 완료 |
| [x] | 서버 command dispatch 경로 | 완료 |
| [x] | Unreal realtime 수신 경로 | 완료 |
| [x] | QR/URL API | 완료 |
| [ ] | command cooldown | 미구현 |
| [ ] | command priority | 미구현 |
| [ ] | duplicate command guard | 미구현 |
| [ ] | conversation log | 챗봇 단계에서 구현 |

### 4.5 문서/시연

| 체크 | 항목 | 상태 |
|---|---|---|
| [x] | 기본 PLAN 문서 | 갱신 완료 |
| [x] | MVP 체크리스트 | 갱신 완료 |
| [x] | Blue Garage 지원 분석 | 작성 완료 |
| [x] | Chatbot MVP 계획 | 작성 완료 |
| [ ] | 최종 실행 가이드 | 필요 |
| [ ] | 데모 시나리오 | 필요 |
| [ ] | 1~2분 시연 영상 | 필요 |
| [ ] | 구조도/다이어그램 | 필요 |

## 5. 다음 작업 우선순위

### 1순위: 패키징 완성도 고정

목표:

```text
exe 실행
-> 원하는 Unreal 레벨 시작
-> 서버 자동 실행
-> 모바일 패널 접속
-> QR/URL 표시
-> Happy/Aged/Classic/Move/Rotate 동작
```

필요 작업:

- `DefaultGame.ini`, `WindowsGame.ini`의 `ServerExePath` 정리
- 실제 패키징 폴더 기준 서버 위치 확정
- Project Settings에서 Game Default Map 확인
- Packaging Maps 목록 확인
- 패키징 후 노트북 단독 테스트

### 2순위: 시연 품질 정리

필요 작업:

- 시연용 레벨/구간 고정
- 시연 카메라 구도 확인
- 감정/맵 변화가 눈에 잘 보이도록 연출 조정
- 모바일 패널 크기 차이 점검
- 1~2분 데모 순서 작성

### 3순위: AI 전시 가이드 챗봇

필요 작업:

- Chat UI
- `/api/chat`
- 전시물 knowledge JSON
- LLM API
- structured output
- command validation
- in-memory RAG
- conversation log JSONL

이 단계가 들어가면 LLM Engineer 지원 근거가 크게 좋아진다.

## 6. 최종 MVP 완료 기준

아래가 모두 되면 MVP 완료로 본다.

- [ ] 패키징 exe 실행
- [ ] 원하는 레벨로 시작
- [ ] 서버 자동 실행
- [ ] 모바일 패널 접속
- [ ] QR 또는 URL 안내
- [ ] 조이스틱 이동
- [ ] 터치 회전
- [ ] Happy 반응
- [ ] Aged/Classic 변화
- [ ] 인사/설명 애니메이션
- [ ] 노트북 단독 재현
- [ ] 1~2분 데모 영상

현재는 기능 상당수가 구현/확인됐지만, 최종 MVP 완료 체크는 “패키징 폴더 기준으로 한 번에 재현되는지”를 기준으로 다시 찍어야 한다.

## 7. 현재 기준 결론

현재 프로젝트는 초기 PLAN.MD보다 실제 배포/현장 접속 측면에서 더 나아갔다.

가장 큰 추가 성과:

- 서버 자동 실행 구조
- 패널 정적 배포
- 노트북 IP 접속
- QR 생성
- 주요 감정/스테이지 반응 확인

가장 큰 남은 리스크:

- 패키징 경로
- 시작 레벨
- 최종 시연 품질
- 데모 영상 부재

따라서 다음 작업은 새 기능을 무작정 늘리기보다, 먼저 패키징 실행 흐름을 완전히 고정하는 것이다. 그다음 LLM/RAG 챗봇을 붙이는 순서가 가장 안전하다.
---

## 8. 2026-05-03 기준 최신 상태

현재 프로젝트는 초기 MVP 체크리스트보다 더 진행된 상태다. 핵심 흐름은 다음과 같이 정리한다.

```text
Windows packaged Unreal exe
-> bundled local ExhibitionServer 실행
-> Mobile Panel static file 제공
-> QR/IP로 같은 Wi-Fi 모바일 접속
-> 이동/회전/전시장 명령/AI 채팅 요청
-> Local ExhibitionServer
-> Azure ExhibitionAiGateway
-> OpenAI Responses API / embedding rerank
-> 답변 streaming + 검증된 Unreal command 실행
```

### 8.1 완료로 갱신할 항목

| 항목 | 상태 | 근거 |
|---|---|---|
| Azure AI Gateway 배포 | 완료 | Azure App Service 도메인으로 동작 확인 |
| OpenAI API key 분리 | 완료 | Steam/로컬 패키지에는 `OPENAI_API_KEY`를 넣지 않고 Azure 환경 변수에서 관리 |
| 로컬 서버의 Gateway 연동 | 완료 | `AiGateway:BaseUrl`, `AiGateway:ClientKey`가 Production 설정에 반영 |
| 모바일 패널 fetch URL 수정 | 완료 | 휴대폰에서 `localhost`가 아니라 실행 PC IP 기준으로 서버 호출 |
| 모바일 패널 build 반영 | 완료 | `Mobile-Panel/dist`를 `ExhibitionServer/wwwroot` 및 packaged server에 반영 |
| AI 채팅 기본 응답 | 완료 | Azure Gateway 경유 응답 확인 |
| multi-turn memory | 완료 | conversation id 기반 이전 대화 전달 구조 구현 |
| native streaming | 완료 | Gateway -> ExhibitionServer -> Mobile Panel까지 stream event 전달 |
| RAG 후보 검색 | 완료 | 전시물 JSON 기반 context 검색 |
| embedding rerank | 완료 | OpenAI embedding 호출은 Gateway 쪽으로 이동 |
| 전시물 metadata | 완료 | Triceratops, Bell X-1, Ritual wine container 계열 JSON 구성 |
| QR 기반 모바일 접속 | 완료 | 노트북 IP 기반 패널 접근 확인 |
| 캐릭터 중복 스폰 방지 정책 | 소스 반영 | 패널 연결이 여러 번 발생해도 active character 1개 유지 구조로 변경 |

### 8.2 Steam 배포 전 남은 체크리스트

| 체크 | 항목 | 판단 |
|---|---|---|
| [ ] | Unreal 재빌드/재패키징 | UE C++ 변경 사항을 exe에 반영해야 함 |
| [ ] | packaged exe 단독 실행 확인 | 다른 PC에서 UE + local server 자동 실행 확인 |
| [ ] | 모바일 QR 접속 확인 | 같은 Wi-Fi 휴대폰에서 `http://PC_IP:5225` 접속 확인 |
| [ ] | AI 채팅 확인 | 다른 PC에서 Azure Gateway 경유 답변 확인 |
| [ ] | 캐릭터 1개 유지 확인 | QR 재접속/새로고침/여러 패널 접속 시 캐릭터가 늘어나지 않는지 확인 |
| [ ] | Windows 방화벽 안내 | 최초 실행 시 private network 허용 필요 가능성 문서화 |
| [ ] | Azure Gateway 운영 상태 확인 | App Service 환경 변수, OpenAI credit, rate limit 확인 |
| [ ] | Steamworks depot 구성 | `Build/Windows` 폴더 기준 업로드 대상 정리 |
| [ ] | store build smoke test | Steam에서 내려받은 빌드로 같은 절차 재현 |

### 8.3 현재 MVP 판단

현재 기능 기준으로 MVP 자체는 거의 완성 단계다. 남은 핵심은 새 기능 구현보다 `Build/Windows` 산출물이 다른 PC에서 그대로 재현되는지 검증하는 것이다.

현재 남은 큰 작업은 다음 순서가 맞다.

1. UE 재패키징
2. 다른 PC에서 packaged exe 단독 실행 테스트
3. 모바일 QR 접속 + AI 채팅 + 캐릭터 1개 유지 확인
4. Steamworks 업로드 대상 폴더 정리
5. Steam depot 업로드 및 설치 테스트

즉, 지금부터의 중심 작업은 “기능 개발”보다 “배포 검증”이다.

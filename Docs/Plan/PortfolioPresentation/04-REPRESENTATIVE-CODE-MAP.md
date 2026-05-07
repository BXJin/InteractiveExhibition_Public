# 04. Representative Code Map

이 문서는 PPT나 면접 설명에서 "어디를 직접 구현했는지"를 빠르게 보여주기 위한 대표 코드 지도다.

## Unreal 패키징 서버 실행

### 파일

- `Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/Launch/ExhibitionServerLauncher.cpp`
- `Client-Unreal/ExhibitionClient/Config/Windows/WindowsGame.ini`

### 설명 포인트

Unreal 실행 시 함께 배포된 ASP.NET Core 서버를 별도 프로세스로 실행한다. 개발 환경 경로가 아니라 패키징된 Windows 폴더 기준으로 서버 실행 파일을 찾도록 구성했다.

### PPT에서 보여줄 포인트

```ini
ServerExePath=ExhibitionServer/ExhibitionServer.exe
```

왜 중요한가:

- 데모 PC에 .NET SDK를 설치하지 않아도 서버 실행 가능
- Unreal 실행 파일 하나를 여는 경험으로 로컬 서버와 모바일 패널까지 함께 시작
- 개발 PC와 배포 PC의 경로 차이를 제거

## QR 코드와 모바일 접속

### 파일

- `Server-AspNet/ExhibitionServer/Application/PanelAccessService.cs`
- `Server-AspNet/ExhibitionServer/Controllers/PanelAccessController.cs`
- `Mobile-Panel/src/panels/exhibition/ExhibitionPanel.tsx`

### 설명 포인트

서버가 현재 노트북의 로컬 IP를 찾아 QR 코드를 생성하고, 모바일 브라우저는 QR 주소의 host를 기준으로 API를 호출한다.

### 왜 이 기술을 선택했나

데모 장소마다 Wi-Fi와 IP가 달라질 수 있으므로 고정 IP나 하드코딩 주소에 의존하면 안 된다. 런타임에 접근 가능한 주소를 만들어야 실제 시연이 안정적이다.

## 모바일 패널 빌드와 정적 파일 서빙

### 파일

- `Mobile-Panel/src/panels/exhibition/`
- `Server-AspNet/ExhibitionServer/wwwroot/`
- `Docs/Plan/BUILD-UPDATE-GUIDE.md`

### 설명 포인트

React/Vite 패널은 `npm run build`로 `dist`를 만들고, 그 결과물을 ASP.NET Core의 `wwwroot`에 복사해 로컬 서버가 직접 제공한다.

### 왜 이 구조를 선택했나

배포 시 Vite dev server를 따로 실행할 필요가 없다. Windows 실행 파일에서 ASP.NET 서버만 실행되면 모바일 패널도 함께 제공된다.

## AI Gateway Client

### 파일

- `Server-AspNet/ExhibitionServer/Application/Chat/AiGatewayClient.cs`
- `Server-AspNet/ExhibitionServer/Application/Chat/AiGatewayOptions.cs`

### 설명 포인트

로컬 서버는 OpenAI를 직접 호출하지 않고 Azure AI Gateway에 요청한다. 이 계층은 배포 앱과 클라우드 AI 서버 사이의 경계다.

### 왜 이 구조를 선택했나

OpenAI API Key를 사용자의 PC에 배포하지 않기 위해서다. 로컬 서버는 Gateway URL과 client key만 알고, 실제 OpenAI Key는 Azure 환경 변수에만 둔다.

## AI Gateway 인증

### 파일

- `Cloud-AI-Gateway/ExhibitionAiGateway/Middleware/AiGatewayAuthenticationMiddleware.cs`
- `Cloud-AI-Gateway/ExhibitionAiGateway/Configuration/GatewayOptions.cs`

### 설명 포인트

AI Gateway는 임의 사용자가 OpenAI 비용을 발생시키지 못하도록 client key 검증을 수행한다.

### 왜 이 기술을 선택했나

클라우드 AI 서버는 인터넷에 공개되므로 최소한의 인증과 요청 제한이 필요하다. 현재는 포트폴리오용 client key 방식이고, 상용 서비스로 확장하면 user session token이나 Steam ticket 검증으로 바꿀 수 있다.

## OpenAI Provider와 Provider 추상화

### 파일

- `Cloud-AI-Gateway/ExhibitionAiGateway/Providers/IAiChatProvider.cs`
- `Cloud-AI-Gateway/ExhibitionAiGateway/Providers/OpenAI/OpenAiResponsesProvider.cs`
- `Cloud-AI-Gateway/ExhibitionAiGateway/Extensions/ServiceCollectionExtensions.cs`

### 설명 포인트

AI 호출을 provider interface 뒤에 숨겨 OpenAI, Gemini, Groq, Local LLM 같은 provider를 나중에 추가할 수 있게 했다.

### 왜 이 구조를 선택했나

AI 모델과 비용 정책은 자주 바뀐다. 특정 provider에 강하게 묶이면 나중에 교체 비용이 커지므로, Gateway 내부에서 provider 전략 패턴을 사용했다.

## Streaming 응답 처리

### 파일

- `Cloud-AI-Gateway/ExhibitionAiGateway/Providers/OpenAI/ReplyStreamExtractor.cs`
- `Cloud-AI-Gateway/ExhibitionAiGateway/Application/AiChatStreamService.cs`
- `Server-AspNet/ExhibitionServer/Application/Chat/AiGatewayClient.cs`
- `Mobile-Panel/src/panels/exhibition/chatApi.ts`

### 설명 포인트

OpenAI의 streaming 응답을 Gateway에서 받고, 로컬 서버와 모바일 패널까지 전달한다. 패널은 전체 응답을 기다리지 않고 chunk 단위로 화면을 갱신한다.

### 왜 이 기술을 선택했나

AI 답변의 실제 완료 시간이 같아도 첫 텍스트가 빨리 보이면 사용자는 훨씬 빠르게 느낀다. 시연형 포트폴리오에서는 이 체감이 중요하다.

## RAG Knowledge Store

### 파일

- `Server-AspNet/ExhibitionServer/Application/Knowledge/ExhibitionKnowledgeStore.cs`
- `Server-AspNet/ExhibitionServer/Application/Knowledge/IExhibitionKnowledgeStore.cs`
- `Server-AspNet/ExhibitionServer/Data/ExhibitionKnowledge/`
- `Cloud-AI-Gateway/ExhibitionAiGateway/Application/Rag/EmbeddingRetrievedContextRanker.cs`

### 설명 포인트

전시물 설명, alias, tag를 JSON으로 관리하고, 로컬 서버에서 후보 문서를 고른 뒤 Gateway에서 embedding 기반 rerank를 수행한다.

### 왜 이 구조를 선택했나

초기 전시물 수가 적을 때는 외부 Vector DB 없이도 빠르게 구현할 수 있다. 동시에 interface를 분리해 나중에 Qdrant, pgvector, Azure AI Search 같은 Vector DB로 교체할 수 있게 했다.

## AI 응답과 Unreal 명령 연결

### 파일

- `Server-AspNet/ExhibitionServer/Application/Chat/ChatGuideService.cs`
- `Shared/Exhibition.Shared/Ai/`
- `Shared/Exhibition.Shared/Commands/`
- `Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/Realtime/ExhibitionRealtimeSubsystem.cpp`

### 설명 포인트

AI 응답은 단순 텍스트만 반환하지 않고, `SuggestedCommands` 형태로 Unreal에서 실행할 수 있는 안전한 명령 후보를 함께 반환한다.

### 왜 이 구조를 선택했나

LLM이 임의 명령을 직접 실행하면 위험하다. 서버에서 허용된 command schema와 whitelist를 통해 Unreal에 전달 가능한 명령만 실행하도록 분리했다.

## 캐릭터 Spawn 관리

### 파일

- `Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Private/Spawn/ExhibitionSpawnManager.cpp`
- `Client-Unreal/ExhibitionClient/Source/ExhibitionClient/Public/Spawn/ExhibitionSpawnManager.h`

### 설명 포인트

QR 재접속이나 패널 중복 접속이 있어도 현재 MVP에서는 한 캐릭터만 유지하는 single-control 구조로 정리했다.

### 왜 이 구조를 선택했나

현재 목표는 안정적인 1인 전시 시연이다. 멀티 유저 확장은 나중에 Party Mode로 분리하는 것이 맞고, 지금은 중복 캐릭터 생성 문제를 막는 것이 우선이다.


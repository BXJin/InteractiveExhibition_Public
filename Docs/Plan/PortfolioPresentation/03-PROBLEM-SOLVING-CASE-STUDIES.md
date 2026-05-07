# 03. Problem Solving Case Studies

## Case 1. 패키징 빌드에서 로컬 서버가 실행되지 않는 문제

### 문제

개발 PC에서는 Unreal 실행 시 ASP.NET 로컬 서버가 정상 실행됐지만, 노트북에 패키징된 Windows 빌드를 옮기면 QR 코드가 생성되지 않고 모바일 패널 접속도 되지 않았다.

### 원인

패키징 설정의 서버 실행 경로가 개발 환경 기준으로 남아 있었다.

```ini
ServerExePath=../../publish/ExhibitionServer.exe
```

개발 PC에는 우연히 해당 경로에 publish 결과물이 있었지만, 노트북의 실제 배포 폴더 구조에는 없었다. 즉, 개발 환경 의존 경로 때문에 배포 PC에서 서버 실행 파일을 찾지 못했다.

### 해결

최종 배포 구조를 기준으로 서버 경로를 고정했다.

```ini
ServerExePath=ExhibitionServer/ExhibitionServer.exe
```

최종 구조는 다음처럼 정리했다.

```text
Build/Windows/
  ExhibitionClient.exe
  ExhibitionClient/
  Engine/
  ExhibitionServer/
    ExhibitionServer.exe
    wwwroot/
```

### 기술 선택 이유

서버를 Unreal 안에 직접 넣지 않고 별도 프로세스로 실행하면 서버 교체와 재배포가 쉽다. 대신 배포 경로가 중요해지므로, 개발 경로가 아니라 패키징 루트 기준 상대 경로로 고정했다.

### 결과

다른 PC에서 Windows 실행 파일을 실행해도 로컬 서버와 QR 코드가 함께 동작하는 구조가 됐다.

## Case 2. 모바일 패널에서 fetch error가 발생한 문제

### 문제

PC 브라우저에서는 AI 채팅과 명령이 동작했지만, 스마트폰으로 QR 코드를 찍어 접속하면 일부 요청에서 `fetch error`가 발생했다.

### 원인

모바일 브라우저에서 `localhost`는 노트북이 아니라 스마트폰 자기 자신을 의미한다. 패널 코드가 `localhost:5225` 같은 주소를 기준으로 요청하면 스마트폰에서는 서버를 찾을 수 없다.

### 해결

패널이 접속한 현재 호스트를 기준으로 API URL을 만들도록 변경했다.

```ts
const host = window.location.hostname;
const baseUrl = `http://${host}:5225`;
```

### 기술 선택 이유

노트북 IP를 고정하기 어렵고, 데모 장소마다 네트워크가 달라질 수 있다. QR 코드로 접속한 주소의 host를 그대로 사용하면 별도 설정 없이 현재 실행 중인 노트북 서버로 요청을 보낼 수 있다.

### 결과

같은 Wi-Fi에 연결된 스마트폰에서 QR 접속 후 모바일 패널 명령과 AI 채팅 요청이 정상 동작했다.

## Case 3. OpenAI API Key를 배포 파일에 넣을 수 없는 문제

### 문제

Steam이나 itch.io에 Windows 빌드를 배포하려면 OpenAI API Key가 실행 파일이나 설정 파일에 포함되면 안 된다. 키가 포함되면 누구나 추출해서 사용할 수 있고 비용 사고로 이어질 수 있다.

### 원인

초기 구조에서는 로컬 서버가 OpenAI embedding이나 chat 호출을 직접 수행할 수 있는 구조였다. 로컬 서버는 최종적으로 사용자 PC에 배포되는 대상이므로 민감한 키를 넣으면 안 된다.

### 해결

OpenAI 호출을 `Cloud-AI-Gateway/ExhibitionAiGateway`로 분리했다.

```text
Packaged App
  -> Local ExhibitionServer
  -> Azure AI Gateway
  -> OpenAI API
```

패키징된 로컬 서버에는 OpenAI API Key가 없고, Azure App Service 환경 변수에만 저장한다. 로컬 서버는 Gateway URL과 client key만 사용한다.

### 기술 선택 이유

실무 배포 구조에서는 외부 API Key를 클라이언트 앱에 넣지 않는다. Gateway를 두면 인증, rate limit, provider 교체, 비용 제어를 중앙에서 처리할 수 있다.

### 결과

Windows 빌드를 다른 PC에서 실행해도 AI 응답은 클라우드 Gateway를 통해 동작하고, OpenAI API Key는 배포 파일에 포함되지 않는다.

## Case 4. AI 응답 대기 시간이 길게 느껴지는 문제

### 문제

AI 응답을 전체 JSON으로 받은 뒤 패널에 표시하면, 사용자가 입력 후 몇 초간 아무 변화가 없어 답답하게 느껴졌다.

### 원인

초기 API 구조는 전체 응답이 완성될 때까지 기다린 뒤 한 번에 반환하는 방식이었다.

```text
request -> wait full response -> render
```

### 해결

OpenAI streaming 응답을 Gateway에서 받아 로컬 서버와 모바일 패널까지 흘려보내는 구조를 추가했다.

```text
request -> first token -> render progressively -> final command parse
```

### 기술 선택 이유

채팅형 AI UX에서는 실제 응답 완료 시간보다 첫 글자가 나타나는 시간이 더 중요하다. Streaming은 모델 속도를 극적으로 줄이지는 않지만, 사용자가 기다리는 체감을 크게 줄인다.

### 결과

모바일 패널에서 AI 답변이 타이핑되듯 표시되어 시연 경험이 좋아졌다.

## Case 5. 일반 전시 질문에 이상한 답변이 나오는 문제

### 문제

사용자가 "전시장에 전시물 종류 뭐야?"처럼 특정 전시물이 아닌 전체 목록 질문을 했을 때, 특정 전시물 하나에 치우친 답변이 나올 수 있었다.

### 원인

RAG 검색이 특정 전시물 유사도 중심으로 동작하면, 전체 목록이나 전시장 안내 의도를 별도로 처리하지 못한다.

### 해결

전시물 alias/tag 기반 검색과 일반 질문 의도 처리를 분리하고, 전시장 전체 설명이나 목록 질문은 catalog 성격으로 처리하도록 보강했다.

### 기술 선택 이유

모든 질문을 embedding similarity 하나로 처리하면 짧고 일반적인 질문에서 오탐이 생길 수 있다. 실무 RAG에서도 intent routing, metadata filtering, threshold 처리가 필요하다.

### 결과

특정 전시물 질문과 전체 전시장 질문을 구분할 수 있는 구조로 확장됐다.

## Case 6. QR 접속 시 캐릭터가 중복 생성되는 문제

### 문제

QR로 모바일 패널에 접속했을 때 캐릭터가 두 개 생성되는 현상이 있었다.

### 원인

패널 접속 이벤트마다 캐릭터를 새로 spawn하는 구조에서는 재접속이나 여러 패널 접속 시 캐릭터가 중복될 수 있다. 또한 기본 Pawn과 서버 연동으로 생성되는 캐릭터가 겹칠 가능성도 있었다.

### 해결

현재 버전은 single-user exhibition mode로 정리해 한 캐릭터만 유지하도록 했다. 이미 존재하는 캐릭터를 재사용하고, 새 접속이 들어와도 무조건 새로 생성하지 않는 방향으로 처리했다.

### 기술 선택 이유

현재 포트폴리오 목표는 1인 시연이다. 4인 멀티패널 구조는 추후 Party Mode로 확장할 수 있지만, 지금은 안정적인 단일 캐릭터 제어가 우선이다.

### 결과

QR 재접속이나 모바일 패널 접근 시 캐릭터 중복 생성 위험을 줄였다.

## Case 7. 1GB 이상 빌드를 itch.io에 올리는 문제

### 문제

Unreal Windows 빌드가 1GB를 넘어서 itch.io 웹 업로드로는 올리기 어려웠다.

### 원인

Unreal 패키징 결과에는 `.ucas`, `.exe`, `.pdb`, 엔진 런타임 파일이 포함된다. 특히 콘텐츠 패키지와 PDB 파일이 용량을 크게 차지했다.

### 해결

itch.io의 `butler` CLI를 사용했다.

```powershell
butler push "C:\Portfolio\InteractiveExhibition\Build\Windows" bxjin/interactive-exhibition:windows
```

### 기술 선택 이유

`butler`는 큰 빌드 업로드와 이후 패치 업로드에 적합하다. 매번 전체 zip을 다시 올리는 방식보다 배포 관리가 쉽다.

### 결과

1.54GB Windows 빌드를 itch.io channel에 업로드했고, 이후 빌드 업데이트도 같은 명령으로 진행할 수 있게 됐다.


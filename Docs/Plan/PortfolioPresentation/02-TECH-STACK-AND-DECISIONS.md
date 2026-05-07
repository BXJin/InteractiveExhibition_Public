# 02. Tech Stack And Decisions

## 전체 기술 구조

```text
Mobile Browser
  -> React Mobile Panel
  -> ASP.NET Core Local Server
  -> Unreal Engine 5 Client

ASP.NET Core Local Server
  -> Azure AI Gateway
  -> OpenAI API
```

## Unreal Engine 5

### 사용 이유

전시 프로젝트의 핵심은 3D 공간, 캐릭터, 애니메이션, 실시간 반응이기 때문에 Unreal Engine 5를 사용했다. 단순 웹 3D보다 캐릭터 제어, 레벨 구성, 애니메이션 블루프린트, 패키징 시연까지 이어가기 쉽다.

### 해결한 문제

- 실시간 캐릭터 이동과 회전
- AI 응답에 따른 캐릭터 애니메이션 재생
- 전시장 분위기 변경
- Windows 패키징 후 서버 자동 실행

### 어필 포인트

Unreal을 단독으로 사용한 것이 아니라, 외부 ASP.NET 서버와 모바일 웹 패널, AI Gateway와 연결했다. 게임 클라이언트가 외부 시스템과 통신하며 상태를 바꾸는 구조를 직접 구현했다는 점이 중요하다.

## ASP.NET Core Local Server

### 사용 이유

Unreal 클라이언트와 모바일 패널 사이의 중계 서버가 필요했다. 모바일 브라우저가 Unreal에 직접 붙는 구조보다, ASP.NET Core 서버를 두면 HTTP API, 정적 파일 서빙, 실시간 메시지, QR 생성, AI Gateway 호출을 한 곳에서 관리할 수 있다.

### 해결한 문제

- 모바일 패널 정적 파일 제공
- QR 코드 생성
- 패널 명령을 Unreal로 전달
- AI Gateway 호출
- Windows self-contained publish
- 패키징된 실행 파일과 함께 배포

### 선택 이유

C# 기반이기 때문에 Unreal C++과 별개로 서버 로직을 안정적으로 분리할 수 있고, `dotnet publish --self-contained`로 .NET 설치 없이 실행 가능한 서버를 만들 수 있다.

## React + Vite Mobile Panel

### 사용 이유

사용자가 별도 앱을 설치하지 않고 스마트폰 브라우저로 바로 접속해야 했다. React는 상태 관리와 UI 컴포넌트 분리에 유리하고, Vite는 빌드 속도가 빠르며 `dist` 결과물을 ASP.NET `wwwroot`에 넣기 쉽다.

### 해결한 문제

- 모바일 조이스틱 UI
- 채팅 입력 UI
- 실시간 명령 버튼
- 로컬 IP 기반 서버 호출
- QR 접속 후 바로 조작 가능한 패널

### 선택 이유

포트폴리오 관점에서도 사용자가 직접 체험할 수 있는 UI가 필요했다. 모바일 패널은 단순 보조 도구가 아니라 전시 인터랙션의 주 입력 장치다.

## Azure AI Gateway

### 사용 이유

OpenAI API Key를 Windows 패키징 파일에 넣으면 Steam/itch.io 배포 시 키가 노출될 수 있다. 그래서 클라우드 Gateway를 별도 프로젝트로 분리해 OpenAI 호출은 서버에서만 수행하도록 구성했다.

### 해결한 문제

- API Key 보호
- AI Provider 교체 가능성
- 인증 키 기반 Gateway 접근 제한
- Rate limit 적용 기반
- OpenAI 모델/embedding 호출 위치 분리

### 선택 이유

실무에서는 클라이언트에 외부 AI API Key를 넣지 않는다. Gateway 구조를 사용하면 비용 제어, 인증, 로깅, Provider 교체, 운영 환경 설정을 중앙에서 관리할 수 있다.

## OpenAI API

### 사용 이유

전시물에 대해 자연어 질문을 받고, 답변을 생성하며, 일부 응답을 구조화된 명령으로 변환해야 했다. OpenAI 모델은 일반 자연어 응답과 JSON 기반 structured output을 함께 다루기 좋다.

### 해결한 문제

- 전시물 설명 생성
- 사용자 질문 의도 분석
- 캐릭터 애니메이션 명령 추천
- 전시장 상태 변경 명령 추천
- Streaming 응답으로 체감 지연 감소

### 선택 이유

로컬 LLM은 배포 PC 사양과 모델 용량 문제가 크고, Steam/itch.io 배포 기준에서도 설치 부담이 크다. 현재 프로젝트에서는 OpenAI API + Gateway 구조가 가장 현실적인 선택이었다.

## RAG / Knowledge JSON

### 사용 이유

AI에게 전시물 정보를 매번 프롬프트에 직접 쓰면 확장성이 떨어진다. 전시물이 늘어날수록 별도 데이터 파일로 관리하고, 질문과 관련 있는 전시물 정보만 AI에 전달하는 구조가 필요했다.

### 해결한 문제

- 전시물별 설명, alias, tag 관리
- 사용자가 다른 이름으로 질문해도 전시물 매칭
- 외부 Vector DB로 확장 가능한 구조
- AI 응답의 근거 데이터 제공

### 선택 이유

초기에는 JSON + in-memory 검색이 빠르고 단순하다. 이후 전시물이 많아지면 같은 인터페이스를 유지한 채 Vector DB로 교체할 수 있게 설계했다.

## Streaming

### 사용 이유

AI 응답을 모두 받은 뒤 표시하면 사용자가 기다리는 시간이 길게 느껴진다. Streaming을 사용하면 첫 토큰이 도착하는 순간부터 패널에 답변이 표시되어 체감 응답성이 좋아진다.

### 해결한 문제

- 전체 응답 대기 시간으로 인한 답답함
- 모바일 패널에서 AI가 멈춘 것처럼 보이는 문제
- 긴 답변 표시 경험 개선

### 선택 이유

실제 AI 제품도 채팅 UI에서는 streaming을 많이 사용한다. 이 프로젝트에서는 포트폴리오 시연 시 "AI가 바로 반응한다"는 인상을 주는 것이 중요했다.

## itch.io + butler

### 사용 이유

Steam은 출시 준비, 심사, 빌드 관리, 상점 세팅 단계가 많다. 현재 포트폴리오 시연 목적에서는 itch.io가 더 빠르게 Windows 빌드를 공유하기 좋다.

### 해결한 문제

- 1GB 이상 Windows 빌드 업로드
- zip 수동 업로드 제한 회피
- 이후 빌드 업데이트 시 diff upload 가능

### 선택 이유

`butler push`는 변경된 파일만 효율적으로 업로드할 수 있어 Unreal 패키징 결과처럼 큰 빌드에 적합하다.


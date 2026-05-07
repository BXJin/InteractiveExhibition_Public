# 06. General Company Pitch

이 문서는 Blue Garage 외 다른 회사에 지원할 때도 쓸 수 있는 범용 포트폴리오 어필 포인트를 정리한다.

## 한 줄 포지셔닝

Unreal 기반 3D 인터랙션, 모바일 웹 컨트롤, ASP.NET Core 서버, Azure AI Gateway, OpenAI RAG 챗봇을 하나의 배포 가능한 체험으로 연결한 end-to-end 프로젝트.

## Full-stack / Web 지원 포인트

### 어필할 내용

- React + Vite 모바일 패널 구현
- ASP.NET Core API 서버 구현
- 정적 파일 배포 구조 구성
- QR 기반 모바일 접속 플로우 구현
- 클라이언트/서버 환경 차이로 생기는 네트워크 문제 해결

### 말할 수 있는 성과

모바일 앱을 따로 만들지 않고도 스마트폰 브라우저에서 전시장 조작이 가능하도록 만들었다. 개발 서버가 아닌 production build를 ASP.NET `wwwroot`에 포함시켜 실제 Windows 패키징 결과물에서도 패널이 동작하게 했다.

## Backend 지원 포인트

### 어필할 내용

- ASP.NET Core 서버 설계
- Unreal과 모바일 패널 사이의 command 중계
- AI Gateway client 구현
- 환경별 설정 분리
- self-contained Windows publish

### 말할 수 있는 성과

로컬 서버를 데모 PC에 설치하는 방식이 아니라, Unreal Windows 빌드 안에 포함해 실행 파일과 함께 동작하도록 구성했다. 배포 PC에 .NET 설치가 없어도 실행 가능하게 만들었다.

## AI Application 지원 포인트

### 어필할 내용

- OpenAI API 연동
- Streaming 응답 처리
- Multi-turn conversation
- RAG 기반 전시물 설명
- Structured command output
- AI 응답과 3D 인터랙션 연결

### 말할 수 있는 성과

AI 챗봇을 단순 Q&A UI로 끝내지 않고, 사용자의 질문을 전시물 context와 결합한 뒤 Unreal 캐릭터 애니메이션과 전시장 상태 변경으로 연결했다.

## Cloud / DevOps 지원 포인트

### 어필할 내용

- Azure App Service 배포
- 환경 변수 기반 secret 관리
- API Gateway 분리
- itch.io butler 배포
- 배포 빌드 구조 정리

### 말할 수 있는 성과

OpenAI API Key를 패키징 파일에 넣지 않기 위해 AI 호출을 Azure Gateway로 분리했다. Windows 배포 앱은 Gateway만 호출하고, 실제 secret은 클라우드 환경 변수로 관리한다.

## Game Client / Unreal 지원 포인트

### 어필할 내용

- Unreal Engine 5 전시장 구성
- 캐릭터 이동/회전 제어
- 외부 서버 명령을 Unreal runtime에 반영
- AI 응답 기반 animation command 처리
- packaged Windows build 검증

### 말할 수 있는 성과

Unreal 프로젝트를 독립 실행 파일로 패키징하고, 외부 ASP.NET 서버 및 모바일 패널과 함께 동작하도록 만들었다. 게임 클라이언트가 서버와 실시간으로 연결되어 사용자 입력과 AI 명령을 반영한다.

## 문제 해결형 자기소개에 쓰기 좋은 문장

프로젝트를 진행하면서 단순 구현보다 배포 환경에서 실제로 깨지는 문제를 많이 해결했다. 예를 들어 개발 PC에서는 서버가 실행됐지만 노트북 배포 빌드에서는 QR 코드가 생성되지 않는 문제가 있었고, 원인을 패키징 경로 차이로 추적해 서버 실행 경로와 최종 폴더 구조를 정리했다.

또한 모바일 패널에서 `localhost`를 사용하면 스마트폰 기준 localhost로 요청이 가는 문제를 확인했고, QR 접속 host를 기준으로 API URL을 구성하도록 수정해 실제 데모 네트워크에서 동작하게 했다.

AI 기능도 단순히 API를 붙이는 데서 끝내지 않고, 배포 시 API Key가 노출되는 문제를 고려해 Azure AI Gateway를 별도 서버로 분리했다. 이 과정에서 local app, cloud gateway, OpenAI provider, RAG data, Unreal command pipeline을 나누어 설계했다.

## PPT에 넣기 좋은 핵심 문장

- "AI 응답을 텍스트에서 끝내지 않고 Unreal 캐릭터 행동과 전시장 상태 변경으로 연결했습니다."
- "OpenAI API Key를 배포 파일에 포함하지 않기 위해 Azure AI Gateway를 분리했습니다."
- "QR 접속, 모바일 패널, 로컬 서버, Unreal 클라이언트를 하나의 Windows 빌드로 묶어 실제 다른 PC에서 시연 가능하게 만들었습니다."
- "RAG 데이터를 JSON 기반으로 시작하되, Vector DB로 확장 가능한 interface 구조를 고려했습니다."
- "Streaming 응답을 적용해 AI 답변의 체감 대기 시간을 줄였습니다."

## 아직 부족하지만 오히려 성장 포인트로 말할 수 있는 것

- 외부 Vector DB 적용은 다음 단계
- LLM evaluation 자동화는 다음 단계
- Fine-tuning은 아직 직접 수행하지 않았지만 로그 기반 학습 데이터 설계 가능
- 동시 다중 패널은 현재 single-control MVP이고, 추후 Party Mode로 확장 계획
- Text-to-motion은 현재 프로젝트에 직접 적용하지 않았고, 별도 NextProject로 분리해 실험 예정


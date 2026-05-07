# 01. Project Overview

## 한 줄 소개

`InteractiveExhibition`은 Unreal Engine 5 기반 3D 전시 공간을 모바일 웹 패널로 조작하고, 클라우드 AI Gateway를 통해 전시물을 설명하는 AI 전시 가이드까지 연결한 인터랙티브 전시 프로젝트다.

## 기획 의도

단순히 3D 맵을 보여주는 프로젝트가 아니라, 사용자가 스마트폰으로 전시장에 접속해 캐릭터를 움직이고, 전시물에 대해 질문하면 AI가 설명과 함께 캐릭터 반응까지 제어하는 구조를 목표로 했다.

핵심 기획은 다음과 같다.

- 별도 모바일 앱 설치 없이 QR 코드로 모바일 패널 접속
- 노트북이나 데모 PC에서 실행하면 로컬 서버와 모바일 패널이 함께 동작
- 전시물 설명은 하드코딩 답변이 아니라 RAG 기반 AI 응답으로 확장
- AI 응답이 Unreal 명령으로 이어져 캐릭터 애니메이션과 전시장 분위기 제어
- OpenAI API Key는 패키징 파일에 포함하지 않고 Azure Gateway에서만 관리
- 최종적으로 itch.io 같은 배포 플랫폼에서 다른 PC로 다운로드 후 시연 가능

## 사용자 흐름

1. 사용자가 Windows 실행 파일을 실행한다.
2. Unreal 클라이언트가 실행되고, 함께 패키징된 ASP.NET Core 로컬 서버가 시작된다.
3. 화면의 QR 코드를 스마트폰으로 스캔한다.
4. 스마트폰 브라우저에서 모바일 패널이 열린다.
5. 사용자는 조이스틱, 회전, 감정 버튼, 채팅 입력으로 전시를 조작한다.
6. 채팅 질문은 로컬 서버를 거쳐 Azure AI Gateway로 전달된다.
7. AI Gateway는 전시물 지식과 OpenAI 응답을 조합해 답변한다.
8. 응답 텍스트는 패널에 표시되고, 필요한 경우 Unreal 캐릭터 애니메이션이나 전시장 상태가 함께 변경된다.

## 현재 구현된 핵심 기능

- Unreal Engine 5 전시장 실행
- ASP.NET Core 로컬 서버 자동 실행
- React + Vite 모바일 패널
- QR 코드 기반 모바일 접속
- 모바일 조이스틱/회전/명령 패널
- OpenAI 기반 AI 전시 가이드
- Azure App Service 기반 AI Gateway
- Gateway Client Key 인증
- AI 응답 Streaming
- Multi-turn 대화 세션
- 전시물 Knowledge JSON
- Alias/keyword 기반 1차 검색
- AI Gateway 측 embedding rerank 구조
- AI 응답 기반 추천 명령/SuggestedCommands
- 캐릭터 애니메이션 명령: `explain`, `wave`, `happy`, `sad`
- 전시장 분위기/맵 상태 변경 명령
- Windows self-contained 서버 publish
- itch.io butler 배포

## 현재 전시물 데이터

현재 RAG/Knowledge 데이터 기준 전시물은 다음 후보를 중심으로 구성했다.

- `Triceratops_horridus_Marsh`
- `Bell_X_1`
- `Ritual_wine_container_draco`

전시물 데이터는 Unreal 배치 이름과 서버 Knowledge ID를 맞춰, 사용자가 자연어로 질문해도 특정 전시물을 찾을 수 있도록 alias와 tag를 함께 관리한다.

## 프로젝트가 보여주는 역량

- Unreal, 웹, 서버, 클라우드 AI를 하나의 체험으로 통합
- 로컬 실행 환경과 클라우드 API 보안을 분리한 구조 설계
- 실제 배포를 고려한 self-contained publish와 itch.io 배포 경험
- AI 응답을 단순 채팅에서 끝내지 않고 실시간 3D 인터랙션으로 연결
- 기능 구현 중 발생한 네트워크, 경로, 배포, 보안 문제를 직접 해결


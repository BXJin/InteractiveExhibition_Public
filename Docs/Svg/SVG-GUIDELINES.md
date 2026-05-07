# SVG 제작 규칙

이 폴더는 Notion 포트폴리오, PPT, PDF export에 넣기 위한 프로젝트 다이어그램을 보관한다.

## 목적

SVG는 영상이나 스크린샷을 대체하는 자료가 아니라, 프로젝트의 구조와 의사결정을 빠르게 이해시키는 보조 자료다.

권장 사용 위치:

- Notion 프로젝트 상세 페이지
- PPT 시스템 구조 설명 슬라이드
- README 또는 기술 문서
- 면접 중 아키텍처 설명 자료

## 기본 원칙

1. 한 SVG에는 하나의 메시지만 담는다.
2. 텍스트는 짧게 쓴다.
3. 구현 세부사항보다 데이터 흐름과 책임 분리를 보여준다.
4. Notion에서 작게 보여도 읽히도록 최소 글자 크기는 14px 이상으로 둔다.
5. 핵심 경로는 굵은 선이나 강조 색으로 구분한다.
6. 배경은 흰색 또는 매우 밝은 회색으로 둔다.
7. 색상은 4~5개 이내로 제한한다.
8. 장식보다 설명력을 우선한다.

## Notion 삽입 기준

Notion에는 Mermaid 원본보다 SVG 또는 PNG export를 넣는 것이 안정적이다.

권장 흐름:

1. SVG 원본은 이 폴더에서 관리한다.
2. Notion에는 SVG 파일을 이미지로 업로드한다.
3. 모바일에서 글자가 너무 작으면 PNG로 한 번 변환해서 업로드한다.

## 추천 다이어그램 구성

Interactive Exhibition은 아래 2개만 우선 사용한다.

- `interactive-exhibition-architecture.svg`
  - 전체 시스템 책임 분리
  - 모바일 패널, 로컬 서버, Unreal 클라이언트, Azure AI Gateway, OpenAI/RAG 연결

- `ai-guide-workflow.svg`
  - 사용자가 질문한 뒤 AI 답변과 Unreal 명령이 생성되는 흐름
  - RAG, streaming, suggested commands, animation command 연결

## 파일 네이밍

파일명은 소문자 kebab-case를 사용한다.

예시:

- `interactive-exhibition-architecture.svg`
- `ai-guide-workflow.svg`
- `deployment-flow.svg`

## PPT에서 사용할 때

PPT 한 장에 다이어그램 하나만 넣는 것이 좋다.

권장 슬라이드 구성:

- 제목: "전체 아키텍처"
- 좌측 또는 중앙: SVG
- 하단: 핵심 문장 2~3개

예시 핵심 문장:

- "Unreal 실행 파일과 ASP.NET 로컬 서버를 함께 패키징했습니다."
- "OpenAI API Key는 배포 파일에 포함하지 않고 Azure AI Gateway에서만 관리합니다."
- "AI 응답은 텍스트 답변과 Unreal 명령으로 분리해 안전하게 처리합니다."

## 수정 기준

SVG를 수정할 때는 아래 항목을 확인한다.

- Notion에서 100% 폭으로 넣었을 때 글자가 읽히는가
- 모바일 화면에서도 구조가 크게 깨지지 않는가
- 화살표 방향이 실제 데이터 흐름과 맞는가
- 각 박스의 책임이 모호하지 않은가
- 코드에 없는 기능을 이미 구현된 것처럼 표현하지 않았는가


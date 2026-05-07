# 전시물 후보 정리

작성일: 2026-04-28

목적:

- AI 전시 가이드 챗봇이 설명할 수 있는 전시물 후보를 정리한다.
- 단순히 보기 좋은 오브젝트가 아니라, RAG/context로 사용할 metadata와 설명이 충분한 자료를 우선한다.
- Unreal 레벨에 배치 가능한 3D 모델 여부, 라이선스, 설명 품질, AI command 연결 가능성을 함께 본다.

## 선택 기준

좋은 후보 조건:

- 라이선스가 명확하다. 가능하면 `CC0`.
- 제목, 제작자/기관, 시대, 재료, 크기, 용도, 상세 설명이 있다.
- 이미지 또는 3D 모델이 있다.
- AI Guide가 설명하기 좋은 스토리가 있다.
- Unreal에서 시각적으로 식별하기 쉽다.
- 감정, 애니메이션, 조명/분위기 command와 연결하기 쉽다.

주의:

- 현재 프로젝트는 전시물 정보를 모델에 fine-tuning하는 구조가 아니다.
- 전시물 metadata를 knowledge JSON으로 만들고, 질문 시 RAG/context로 prompt에 넣는 방식이다.
- 따라서 "AI가 학습하기 좋은 전시물"이라기보다 "RAG context로 넣기 좋은 전시물"을 고르는 것이 맞다.

## 추천 소스

### Smithsonian Open Access / Smithsonian 3D

링크:

- https://www.si.edu/openaccess
- https://3d.si.edu/collections/openaccesshighlights

장점:

- 2D/3D 자료가 많다.
- 많은 자료가 `CC0`로 제공된다.
- 과학, 역사, 기술, 자연사 오브젝트가 많아 전시형 데모에 적합하다.
- 3D 모델과 상세 metadata가 함께 있는 경우가 많다.

주의:

- 3D 모델 polygon 수가 높을 수 있으므로 Unreal import 후 LOD/Nanite/scale 조정이 필요할 수 있다.

### The Metropolitan Museum of Art Open Access

링크:

- https://www.metmuseum.org/en/hubs/open-access

장점:

- public domain 작품 이미지와 기본 metadata가 많다.
- 회화, 조각, 유물 후보가 많다.
- 일부 3D 모델도 제공한다.

주의:

- 모든 작품에 긴 설명이 있는 것은 아니다.
- 3D 모델 후보는 Smithsonian보다 직접 탐색이 더 필요하다.

### Art Institute of Chicago API

링크:

- https://api.artic.edu/docs/
- https://www.artic.edu/open-access/public-api

장점:

- API가 잘 정리되어 있다.
- title, artist, date, style, medium, description 등 RAG용 metadata가 좋다.
- IIIF 이미지 URL을 만들기 쉽다.

주의:

- `description` 필드는 CC-BY 성격이므로 출처 표기를 해야 한다.
- 3D 오브젝트보다는 2D artwork/metadata 중심이다.

## 후보 1. 1903 Wright Flyer

출처:

- Smithsonian object page: https://www.si.edu/object/nasm_A19610048000
- Smithsonian 3D page: https://3d.si.edu/object/3d/1903-wright-flyer%3Ad8c62e5e-4ebc-11ea-b77f-2e728ce88125
- Sketchfab download page: https://sketchfab.com/3d-models/1903-wright-flyer-c91467735c6743af872e65107a79beda

라이선스:

- `CC0`
- Smithsonian public domain media로 표시됨
- 그래도 포트폴리오에는 `Source: Smithsonian National Air and Space Museum` 표기를 권장

3D 모델:

- 있음
- Sketchfab 기준 약 `296.7k triangles`, `149.4k vertices`
- Unreal에 import 가능하지만, 노트북/패키징 데모 기준으로는 성능 확인 필요

metadata 품질:

- 매우 좋음
- physical description, summary, long description, brief description, date, materials, dimensions, location, exhibition, record id가 있음

RAG에 좋은 이유:

- 첫 동력 비행이라는 명확한 스토리가 있다.
- 연구개발, 실험, 실패, 개선, 기술 혁신이라는 설명 포인트가 풍부하다.
- "왜 중요한가?", "어떻게 날았나?", "라이트 형제가 뭘 발명했나?" 같은 질문에 답하기 좋다.

Unreal 연출 아이디어:

- 전시관 중앙에 비행기 모델을 크게 배치
- `style_modern` 또는 `style_classic`과 연결
- spotlight on/off로 항공기 강조
- AI Guide가 설명할 때 캐릭터 `explain` animation 실행
- "놀랍게 소개해줘" 요청 시 `surprise` emotion 사용

knowledge JSON 초안:

```json
{
  "id": "artifact_wright_flyer_1903",
  "title": "1903 Wright Flyer",
  "zone": "technology_hall",
  "summary": "라이트 형제가 1903년 12월 17일 키티호크에서 비행에 성공한 최초의 동력 비행기입니다.",
  "description": "1903 Wright Flyer는 Wilbur와 Orville Wright가 4년간의 연구와 실험 끝에 완성한 동력 비행기입니다. 1903년 12월 17일 North Carolina의 Kitty Hawk에서 Orville Wright가 조종한 첫 비행은 12초 동안 36m를 날았고, 같은 날 Wilbur Wright가 조종한 가장 긴 비행은 59초 동안 255.6m를 날았습니다. 이 항공기는 단순한 첫 비행 기록뿐 아니라, 풍동 실험, 비행 테스트, 날개 비틀림 제어 등 현대 항공공학의 기반을 세운 사례로 평가됩니다.",
  "tags": ["aviation", "engineering", "history", "wright brothers", "flight", "technology"],
  "recommendedEmotion": "surprise",
  "recommendedAnimation": "explain",
  "recommendedStageEvent": "style_modern",
  "source": "Smithsonian National Air and Space Museum",
  "license": "CC0"
}
```

판단:

- 1번 전시물로 채택 추천
- LLM/RAG와 3D object demo 양쪽에 모두 좋다.

## 후보 2. Apollo 11 Command Module

출처:

- Smithsonian Open Access / Smithsonian 3D에서 탐색 가능
- 후보 검색 키워드: `Apollo 11 Command Module Smithsonian 3D`

라이선스:

- Smithsonian 3D Open Access 후보는 대체로 `CC0` 여부 확인 필요

3D 모델:

- Smithsonian 3D에 관련 모델이 존재하는 후보

metadata 품질:

- 높을 가능성이 크다.
- 우주 탐사, 달 착륙, NASA, 기술사 context가 풍부하다.

RAG에 좋은 이유:

- 사용자가 "이건 왜 중요한가?", "어떤 임무에 쓰였나?", "라이트 플라이어와 비교해줘" 같은 질문을 하기 좋다.
- Wright Flyer와 함께 "비행의 시작 -> 우주 탐사"라는 기술 발전 서사를 만들 수 있다.

Unreal 연출 아이디어:

- technology hall의 두 번째 전시물로 배치
- `style_modern`, `spotOn`, `surprise`와 연결
- Wright Flyer와 비교 설명하는 멀티턴 질문에 적합

판단:

- 2번 후보로 좋음
- 단, 실제 3D model download와 poly count 확인 필요

## 후보 3. Triceratops horridus

출처:

- Smithsonian 3D Open Access Highlights
- 후보 검색 키워드: `Triceratops horridus Smithsonian 3D`

라이선스:

- Smithsonian Open Access의 3D highlight 후보는 `CC0` 여부 확인 필요

3D 모델:

- 있음

metadata 품질:

- 자연사/고생물 설명이 가능하다.
- 생물학, 화석, 지질 시대, 박물관 전시 context로 확장하기 좋다.

RAG에 좋은 이유:

- 기술 오브젝트와 전혀 다른 성격의 자연사 전시물이라 검색/분류 테스트에 좋다.
- "비행기 말고 생물 전시물 설명해줘" 같은 질문에서 RAG 차이를 확인하기 좋다.

Unreal 연출 아이디어:

- 자연사 구역 또는 어두운 spot light 전시
- `surprise`, `spotOn`, `explain`과 연결
- 큰 스케일 오브젝트라 시각적으로 강함

판단:

- 3번 후보로 좋음
- 모델 크기와 최적화 난이도 확인 필요

## 후보 4. Mammuthus primigenius

출처:

- Smithsonian 3D Open Access Highlights
- 후보 검색 키워드: `Mammuthus primigenius Smithsonian 3D`

라이선스:

- `CC0` 여부 확인 필요

3D 모델:

- 있음

metadata 품질:

- 자연사/멸종 동물/기후 변화/화석 설명으로 확장 가능

RAG에 좋은 이유:

- Triceratops와 비슷한 자연사 계열이지만, 포유류/빙하기/멸종 context로 차별화 가능

Unreal 연출 아이디어:

- `style_aged`, `spotOn`, `surprise`
- 차분한 설명에는 `neutral` + `explain`

판단:

- Triceratops와 둘 중 하나만 선택해도 충분
- 자연사 전시물 1개만 필요하면 시각적 임팩트가 큰 쪽을 선택

## 후보 5. Morse-Vail Telegraph Key

출처:

- Smithsonian Open Access Highlights
- 후보 검색 키워드: `Morse-Vail Telegraph Key Smithsonian 3D`

라이선스:

- `CC0` 여부 확인 필요

3D 모델:

- Smithsonian 3D highlight 후보로 존재

metadata 품질:

- 통신 기술, 전신, 정보 전달, 현대 네트워크의 역사로 설명 가능

RAG에 좋은 이유:

- Wright Flyer, Apollo와 함께 "기술 혁신 전시" 테마로 묶기 좋다.
- 작은 오브젝트라 전시대/조명 연출에 적합하다.

Unreal 연출 아이디어:

- 유리 진열대 위 소형 오브젝트
- `style_classic`, `spotOn`, `explain`
- "소리를 내며 설명해줘" 같은 향후 sound/voice 확장에도 연결 가능

판단:

- 기술사 전시물로 좋음
- 3D 모델의 시각적 임팩트는 Wright Flyer/Apollo보다 약할 수 있음

## 후보 6. Ritual wine container 또는 Buddha statue

출처:

- Smithsonian 3D Open Access Highlights
- 후보 검색 키워드:
  - `Ritual wine container fangyi Smithsonian 3D`
  - `Buddha draped in robes Smithsonian 3D`

라이선스:

- `CC0` 여부 확인 필요

3D 모델:

- Smithsonian 3D 후보로 존재

metadata 품질:

- 문화권, 의례, 재료, 상징성 설명에 좋다.

RAG에 좋은 이유:

- 기술/과학 계열 전시물만 있으면 전시가 단조로워질 수 있다.
- 문화재/종교/상징 해석이 들어가면 AI Guide의 설명 다양성이 좋아진다.

Unreal 연출 아이디어:

- `style_classic`, `style_aged`, `bow`, `spotOn`
- 캐릭터가 차분하게 설명하거나 고개 숙이는 animation과 잘 맞음

판단:

- 전시관 분위기 다양화용으로 좋다.
- 문화재 설명은 표현을 조심해야 하므로 source 기반 설명을 유지하는 것이 중요하다.

## 1차 추천 조합

가장 균형 좋은 3개:

1. `1903 Wright Flyer`
   - 기술사, 3D 모델, metadata 풍부
2. `Triceratops horridus` 또는 `Mammuthus primigenius`
   - 자연사, 큰 3D 오브젝트, 시각적 임팩트
3. `Buddha statue` 또는 `Ritual wine container`
   - 문화재, classic/aged 분위기, 차분한 설명

기술 전시관 느낌으로 가고 싶다면:

1. `1903 Wright Flyer`
2. `Apollo 11 Command Module`
3. `Morse-Vail Telegraph Key`

AI Guide/RAG 테스트 관점에서 좋은 조합:

1. 기술 오브젝트 1개
2. 자연사 오브젝트 1개
3. 문화재 오브젝트 1개

이렇게 해야 질문에 따라 retrieved source가 잘 갈라지는지 확인하기 쉽다.

## 내일 작업 순서 제안

1. Smithsonian 3D에서 최종 후보 3개 다운로드 가능 여부 확인
2. 각 후보의 polygon 수, 파일 포맷, texture 포함 여부 확인
3. Unreal import 테스트
4. scale/rotation/material 정리
5. `Server-AspNet/ExhibitionServer/Data/ExhibitionKnowledge`에 knowledge JSON 3개 작성
6. AI Guide 질문 테스트
7. 전시물별 recommendedEmotion/recommendedAnimation/recommendedStageEvent 조정

## knowledge 작성 템플릿

```json
{
  "id": "artifact_unique_id",
  "title": "Display Title",
  "zone": "main_hall",
  "summary": "짧은 한 문장 요약",
  "description": "AI Guide가 설명에 사용할 상세 설명. 역사적 의미, 형태, 재료, 사용 맥락, 관람 포인트를 포함한다.",
  "tags": ["tag1", "tag2", "tag3"],
  "recommendedEmotion": "neutral",
  "recommendedAnimation": "explain",
  "recommendedStageEvent": "spotOn",
  "source": "Source institution name",
  "sourceUrl": "https://...",
  "license": "CC0"
}
```

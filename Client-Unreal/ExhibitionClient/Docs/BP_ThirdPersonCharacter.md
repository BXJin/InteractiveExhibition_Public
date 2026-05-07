# BP_ThirdPersonCharacter — Blueprint 로직 문서

Blueprint 파일은 바이너리(`.uasset`)이므로 핵심 로직을 텍스트로 정리합니다.

---

## 감정·애니메이션 명령 처리 흐름

서버에서 `setEmotion` / `playAnimation` 명령을 수신하면:

```
ExhibitionRealtimeSubsystem (WebSocket 수신)
    │
    ▼  OnSetEmotion Delegate
BP_ThirdPersonCharacter
    │
    ▼  ExecuteReaction(EmotionType)
    ├─ DT_ReactionData에서 ST_ReactionData 조회 (EmotionType → 행 이름)
    ├─ SetCameraActive(IsFace: true)     ← 카메라 정면 전환 (모든 감정 명령 시 자동)
    ├─ SetCurrentReactionData(row)
    └─ ApplyReactionToAnimInstance()
           ├─ 모프 타겟 적용 (Mouth_Smile, Eye_Squint 등)
           └─ Body Montage 재생 (있는 경우)
```

---

## DT_ReactionData 구조

**DataTable 에셋**: `Content/Characters/DT_ReactionData`  
**행 구조체**: `ST_ReactionData`

| 행 이름 | 감정 | 특징 |
|---------|------|------|
| `Neutral` | 기본 (옅은 미소) | Mouth_Smile=0.3, Eye_Squint=0.25 |
| `Greeting` | 인사 | 밝은 미소 |
| `Explaining` | 설명 중 | 집중된 표정 |
| `Curious` | 호기심 | 눈썹 약간 올림 |
| `Thinking` | 생각 중 | 눈썹 모음 |
| `HappyReaction` | 기쁨 | 크게 웃음 |
| `SurprisedReaction` | 놀람 | Eye_Wide |
| `SadReaction` | 슬픔 | Mouth_Frown |
| `AngryReaction` | 화남 | Brow_Drop, Brow_Compress |

### ST_ReactionData 필드

```
EmotionType    : byte (EmotionType 열거형)
FacePreset     : ST_FacePreset (모프 타겟 값 묶음)
BodyMontage    : AnimMontage (선택, 없으면 idle 유지)
```

### ST_FacePreset 모프 타겟

```
Mouth_Smile      : 0.0 ~ 1.0
Eye_Squint       : 0.0 ~ 1.0
Mouth_Frown      : 0.0 ~ 1.0
Mouth_Press      : 0.0 ~ 1.0
Brow_Raise_Inner : 0.0 ~ 1.0
Eye_Wide         : 0.0 ~ 1.0
Brow_Drop        : 0.0 ~ 1.0
Brow_Compress    : 0.0 ~ 1.0
Brow_Raise_Outer : 0.0 ~ 1.0
Mouth_Close      : 0.0 ~ 1.0
```

---

## 카메라 전환 (SetCameraActive)

```
SetCameraActive(IsFace: true)
    → 정면 카메라(Face Camera) 활성화
    → 일정 시간 후 자동으로 기본 3인칭 카메라로 복귀

SetCameraActive(IsFace: false)
    → 3인칭 카메라로 즉시 복귀
```

모든 `setEmotion` 명령 수신 시 `SetCameraActive(true)`가 자동 호출됩니다.  
별도 `cameraFront` 명령이 필요하지 않습니다.

---

## 서버가 보내는 명령 예시

```json
{
  "suggestedCommands": [
    {
      "type": "setEmotion",
      "characterId": "Character_01",
      "emotion": "explaining"
    },
    {
      "type": "playAnimation",
      "characterId": "Character_01",
      "animation": "explain"
    }
  ]
}
```

**감정 타입 전체 목록**: `greeting`, `explaining`, `curious`, `thinking`,  
`happy`, `surprise`, `sad`, `angry`, `neutral`

**애니메이션 타입**: `wave`, `bow`, `clap`, `explain`, `idle`

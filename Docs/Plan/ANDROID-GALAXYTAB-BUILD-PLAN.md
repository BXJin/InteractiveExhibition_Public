# Galaxy Tab Android 빌드 및 모바일 컨트롤 계획

작성일: 2026-04-23

---

## 목표

```
갤럭시 탭 (UE5 게임 실행 — 전시 디스플레이)
        ↑ WebSocket
     ASP.NET 서버 (PC 또는 별도 호스트)
        ↑ SignalR
  핸드폰 브라우저 (모바일 패널 — 조작 컨트롤러)
```

---

## 가능 여부: YES (단, 조건 있음)

### ✅ 가능한 것

| 항목 | 근거 |
|------|------|
| UE5 → Android(Galaxy Tab) 패키징 | UE5 공식 Android 빌드 지원 |
| 패널에서 갤탭 캐릭터 조종 | 기존 SignalR 파이프라인 그대로 사용 |
| 같은 Wi-Fi 내 핸드폰 → 서버 → 갤탭 통신 | LAN 환경이면 동작 |

### ⚠️ 주의사항 / 추가 작업 필요한 것

| 항목 | 설명 |
|------|------|
| ASP.NET 서버는 PC에서 실행해야 함 | Android에서 .NET 서버 실행 불가 — 서버는 반드시 PC 또는 클라우드에서 실행 |
| UE 자동 서버 실행 기능 비활성화 필요 | `ExhibitionServerLauncher`가 EXE를 실행하는 로직 — Android에서 동작 안 함, 플랫폼 분기 필요 |
| WebSocket URL 하드코딩 문제 | 현재 `ws://127.0.0.1:5225` — Android에서는 PC IP로 변경해야 함 |
| Android SDK 셋업 필요 | UE5 Android 빌드 환경 구성 (Android Studio, NDK, SDK) |
| 갤탭 성능 고려 | 4096 해상도 머티리얼 × 566 액터 — 모바일 최적화 필요할 수 있음 |
| 입력 방식 변경 | 갤탭 터치 입력 vs 기존 키보드 입력 — 갤탭에서는 키 5/6/7 스타일 전환 불가 |

---

## 구성 방법 (권장 토폴로지)

```
[갤럭시 탭]                [PC]                  [핸드폰]
UE5 Android 앱    ←WS→   ASP.NET 서버   ←SignalR→  브라우저
                          + Vite 패널 서버
```

### 네트워크 조건
- 갤탭, PC, 핸드폰이 **같은 Wi-Fi**에 연결되어야 함
- PC의 로컬 IP 예: `192.168.1.10`
- 갤탭의 WebSocket URL: `ws://192.168.1.10:5225/ws/unreal`
- 핸드폰의 패널 URL: `http://192.168.1.10:3001`

---

## 단계별 구현 계획

### Step 1. Android 빌드 환경 구성

1. Android Studio 설치
2. UE5 → Edit → Plugins → Android 활성화 확인
3. Project Settings → Android → SDK 경로 설정
4. Galaxy Tab ADB 연결 확인 (`adb devices`)

### Step 2. 플랫폼 분기 처리 (C++ 수정)

**ExhibitionServerLauncher** — Android에서는 서버 실행 스킵:

```cpp
void UExhibitionServerLauncher::LaunchServer()
{
#if PLATFORM_ANDROID
    UE_LOG(LogExhibitionLauncher, Log, TEXT("Android: skipping local server launch."));
    return;
#endif
    // 기존 PC 로직...
}
```

**WebSocket URL** — Android에서는 `DefaultGame.ini` 또는 런타임 설정으로 PC IP 지정:

```ini
; DefaultGame.ini (Android 전용 오버라이드)
[/Script/ExhibitionClient.ExhibitionRealtimeSubsystem]
WebSocketUrl=ws://192.168.1.10:5225/ws/unreal
```

또는 UE의 플랫폼별 Config 사용:
```
Config/Android/AndroidGame.ini
```

### Step 3. 모바일 그래픽 최적화 (선택)

- 4096 머티리얼 → 모바일에서 2048 이하 권장
- Mobile HDR 비활성화 고려
- `r.Mobile.ShadingPath=0` (deferred → forward 렌더링)

### Step 4. 패키징 및 배포

```bash
# UE 에디터
File → Package Project → Android → Android (ETC2)
# 또는 Android (ASTC) — 최신 갤탭 권장
```

설치:
```bash
adb install ExhibitionClient.apk
```

### Step 5. 스타일 전환 UI (갤탭용)

키 5/6/7은 Android에서 동작 안 함.  
→ Level Blueprint의 키 바인딩을 On-Screen Button 또는 패널 명령으로 대체 필요.  
→ 이미 구현한 `switchStyle` 패널 버튼이 이 역할을 담당.

---

## 예상 이슈 및 해결 방법

| 이슈 | 해결 |
|------|------|
| 갤탭에서 서버 연결 실패 | PC IP로 WebSocket URL 변경, 방화벽 5225 포트 허용 |
| 앱 설치 후 블랙스크린 | Android Vulkan/ES3.1 설정 확인 |
| 4096 텍스처 로딩 느림 | Streaming Pool Size 증가 또는 2048 텍스처 사용 |
| 패널에서 갤탭 접근 안 됨 | CORS 설정 확인, 서버 바인딩 IP `0.0.0.0` 확인 |
| 캐릭터 스폰 안 됨 | OnPanelConnected 이벤트가 WebSocket 연결 후 발행되는지 확인 |

---

## 현재 구현 대비 변경 필요 목록

- [ ] `ExhibitionServerLauncher` Android 플랫폼 분기 추가
- [ ] WebSocket URL 런타임 설정 가능하도록 변경 (하드코딩 제거)
- [ ] Android SDK 빌드 환경 구성
- [ ] 모바일 그래픽 설정 최적화 프로파일 작성
- [ ] 키 입력 스타일 전환 → 패널 버튼으로 완전 대체 확인

---

## 결론

**충분히 가능하다.**  
핵심 파이프라인(SignalR → UE WebSocket → 캐릭터 제어)은 플랫폼 무관하게 동작한다.  
주요 작업은 서버 자동실행 로직의 Android 분기와 WebSocket URL의 동적 설정뿐이며,  
나머지는 UE5의 표준 Android 패키징 프로세스를 따른다.

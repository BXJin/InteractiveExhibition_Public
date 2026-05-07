# 배포 전략 — Steam / Play Store / 갤탭 구성

작성일: 2026-04-24

---

## 현재 구성 (개발 기준)

```
PC (노트북/데스크탑)
  └─ UE 에디터 실행
  └─ .NET 서버 자동 실행 (ExhibitionServerLauncher)
  └─ npm run dev (Vite 패널 서버 :3001)

폰/갤탭 브라우저
  └─ http://PC_IP:3001 → 모바일 패널 접속 → 캐릭터 조종
```

---

## 구성 1 — 갤탭(UE Android) + 폰(패널) + PC(서버)

```
갤탭:     UE Android APK 실행 (3D 씬 디스플레이)
PC:       .NET 서버 실행 + 패널 파일 서빙
폰 브라우저: http://PC_IP:3001 → 패널 → 캐릭터 조종
```

### 바꿔야 할 것

| 항목 | 내용 | 난이도 |
|------|------|--------|
| ExhibitionServerLauncher Android 분기 | `#if PLATFORM_ANDROID` 로 서버 자동실행 스킵 | 낮음 |
| WebSocket URL | `ws://127.0.0.1:5225` → `ws://PC_IP:5225` (DefaultGame.ini Android 오버라이드) | 낮음 |
| Android SDK 환경 구성 | Android Studio, NDK, UE5 Android 플러그인 설정 | 중간 |
| UE Android 패키징 | File → Package → Android (ASTC) | 낮음 |
| APK 갤탭 설치 | `adb install` 또는 직접 전송 | 낮음 |

### 하지 않아도 되는 것

- .NET 서버 코드 변경 없음
- 모바일 패널 코드 변경 없음
- Play Store 등록 불필요 (APK 직접 설치)

---

## 구성 2 — Steam 배포 (PC)

```
플레이어 PC:  Steam → UE 게임 실행 → .NET 서버 자동 실행 → 패널 자동 서빙
폰 브라우저:  http://PC_IP → 패널 접속 → 캐릭터 조종
```

### 바꿔야 할 것

| 항목 | 내용 | 난이도 |
|------|------|--------|
| 패널 static 빌드 서빙 | `npm run build` 결과물을 .NET 서버가 직접 서빙 → Vite 별도 실행 불필요 | 중간 |
| 서버 EXE 번들링 | 게임 폴더에 .NET 서버 EXE + 패널 빌드 파일 포함 | 낮음 |
| publish 경로 정리 | 현재 `../../publish/ExhibitionServer.exe` 경로를 게임 패키지 내부로 정리 | 낮음 |
| Steam SDK 연동 | Steamworks SDK, UE Steam 플러그인 설정 | 중간 |
| Steam 개발자 등록 | Steam Direct ($100) | 낮음 (비용) |

### .NET 서버가 패널을 직접 서빙하는 방법

`Program.cs`에 static file middleware 추가:

```csharp
// 빌드된 패널 파일(dist/)을 서버가 직접 서빙
app.UseDefaultFiles();
app.UseStaticFiles();
```

빌드 폴더 구조:
```
publish/
  ExhibitionServer.exe
  wwwroot/          ← npm run build 결과물 (dist/) 복사
    index.html
    assets/
```

이렇게 하면 `http://localhost:5225`로 패널 접속 가능 → Vite 불필요.

### 하지 않아도 되는 것

- 서버 구조 변경 없음
- Android 관련 작업 없음
- 패널 코드 변경 없음

---

## 구성 3 — Play Store 배포 (Android)

```
갤탭:  Play Store → UE Android 앱 설치 → 실행
                    ↓
              내장 서버 필요 (외부 PC 없이 동작)
폰 브라우저: 갤탭 IP → 패널 접속
```

### 현재 왜 어려운가

Play Store로 배포하면 설치한 사람 모두 서버가 필요한데, PC 서버에 연결하는 것은 일반 사용자에게 불가능.

**선결 조건: Option 2 (UE 내장 WebSocket 서버) 구현 필요**

UE C++ `IWebSocketServer` 모듈로 서버를 UE 안에 내장해야 외부 PC 없이 동작 가능.

### 구현해야 할 것

| 항목 | 내용 | 난이도 |
|------|------|--------|
| UE 내장 WebSocket 서버 | C++ `IWebSocketServer` 구현 — 현재 .NET 서버 역할 대체 | 높음 |
| 패널 static 파일 내장 | UE Content 폴더에 패널 빌드 파일 포함, HTTP로 서빙 | 높음 |
| ExhibitionRealtimeSubsystem 방향 전환 | 현재 WebSocket Client → WebSocket Server로 역할 변경 | 높음 |
| Android 패키징 | 구성 1과 동일 | 중간 |
| Play Store 등록 | Google Play Console ($25), 심사, 정책 준수 | 중간 |

### 결론

현재 구조에서 Play Store 배포는 **Phase 2 이후 과제**.  
Phase 2 문서의 "Client App = 게임씬 + 패널 제공 주체 + 로컬 런타임" 구조가 완성된 후에 의미가 생김.

---

## 우선순위 로드맵

```
지금          → 구성 1 준비 (Android 빌드 환경, WebSocket URL 분기)
MVP 완성 후   → 구성 2 준비 (Steam, 패널 static 서빙)
Phase 2 이후  → 구성 3 준비 (UE 내장 서버, Play Store)
```

---

## 변경 필요 파일 요약

### 구성 1 (갤탭 Android) 기준

| 파일 | 변경 내용 |
|------|-----------|
| `Source/.../ExhibitionServerLauncher.cpp` | `#if PLATFORM_ANDROID` 분기 추가 |
| `Config/Android/AndroidGame.ini` | WebSocket URL PC IP로 오버라이드 |
| UE Project Settings | Android SDK 경로, 패키징 설정 |

### 구성 2 (Steam) 기준

| 파일 | 변경 내용 |
|------|-----------|
| `Server-AspNet/ExhibitionServer/Program.cs` | static file serving 추가 |
| `Mobile-Panel/package.json` | build 경로를 서버 wwwroot로 지정 |
| UE Project Settings | Steam 플러그인 활성화 |
## AI Gateway / Embedding RAG 배포 주의

Steam 빌드에는 `OPENAI_API_KEY`를 포함하지 않는다. 로컬 `ExhibitionServer`는 전시물 JSON에서 후보 context만 선택하고, OpenAI embedding rerank와 LLM 호출은 클라우드 `ExhibitionAiGateway`가 담당한다.

Steam 빌드에 포함 가능한 값:

```text
AI_GATEWAY_URL=https://<gateway-name>.azurewebsites.net
AI_GATEWAY_CLIENT_KEY=<gateway 접근 제한용 키>
```

Steam 빌드에 포함하면 안 되는 값:

```text
OPENAI_API_KEY
GEMINI_API_KEY
GROQ_API_KEY
```

Azure App Service 쪽에만 설정할 값:

```text
OPENAI_API_KEY
AI_GATEWAY_CLIENT_KEY
OPENAI_MODEL
OPENAI_EMBEDDING_MODEL
```

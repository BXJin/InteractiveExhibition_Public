# 빌드 업데이트 가이드

작성일: 2026-04-26

---

## 폴더 구조 이해

```
InteractiveExhibition/
├── Mobile-Panel/                  ← React 패널 소스
├── Server-AspNet/ExhibitionServer/ ← ASP.NET 서버 소스
│   └── wwwroot/                   ← 패널 빌드 파일 (dist/ 복사본)
├── Client-Unreal/ExhibitionClient/ ← UE5 소스
├── scripts/
│   ├── copy-panel.sh              ← dist/ → wwwroot/ 복사 스크립트
│   ├── publish-server.bat         ← 서버 publish 스크립트
│   └── mcp/
│       ├── add_vars.sh            ← UE BP 변수 추가 (MCP 전용)
│       └── set_cdo_defaults.sh    ← UE CDO 기본값 설정 (MCP 전용)
└── Build/
    ├── Windows/                   ← UE 패키지 출력 (ExhibitionClient.exe)
    │   └── ExhibitionClient/      ← UE ProjectDir (패키지 기준)
    └── publish/                   ← ASP.NET 서버 publish 출력
        ├── ExhibitionServer.exe
        ├── wwwroot/               ← 패널 정적 파일 (폰 브라우저에서 접속)
        └── appsettings.json
```

### 경로 관계

`ExhibitionClient.exe` 실행 시 서버 EXE를 찾는 경로:

```
Build/Windows/ExhibitionClient/  (ProjectDir)
  ../../publish/ExhibitionServer.exe
= Build/publish/ExhibitionServer.exe  ✅
```

---

## 컴포넌트별 업데이트 방법

### 1. Mobile Panel 변경 시 (React/TypeScript)

소스 파일 수정 후 아래 순서로 실행:

```bash
# Step 1: 패널 빌드
cd Mobile-Panel
npm run build
# → Mobile-Panel/dist/ 생성

# Step 2: wwwroot 동기화 (어디서든 실행 가능)
bash scripts/copy-panel.sh
# → Server-AspNet/ExhibitionServer/wwwroot/ 업데이트

# Step 3: 서버 publish (wwwroot 포함, 어디서든 실행 가능)
scripts\publish-server.bat
# → Build/publish/ 업데이트 (ExhibitionServer.exe + wwwroot/)
```

> **왜 Step 2가 필요한가?**
> `publish-server.bat`는 서버 프로젝트 내의 `wwwroot/`를 포함해서 publish 한다.
> `dist/`를 직접 가져가는 게 아니라 서버 프로젝트의 `wwwroot/`를 가져가므로
> 먼저 `wwwroot/`에 복사해야 한다.

---

### 2. ASP.NET 서버 변경 시 (C#)

```bash
scripts\publish-server.bat
# → Build/publish/ 업데이트
```

패널은 변경 없으므로 Step 1, 2 생략 가능.
단, 패널 파일이 `Build/publish/wwwroot/`에 있는지 확인 (처음 한 번은 Step 1~3 전체 필요).

---

### 3. UE5 변경 시 (Blueprint / C++)

UE 에디터에서 직접 패키징:

```
UE Editor → Platforms → Windows → Package Project
출력 경로: C:\Portfolio\InteractiveExhibition\Build\Windows
```

- 기존 파일 덮어쓰기됨
- `Build/publish/`는 영향 없음 (서버/패널 재publish 불필요)
- C++ 변경 시 에디터에서 빌드(Ctrl+Alt+F11) 후 패키징

---

## 전체 클린 빌드 (모든 컴포넌트 변경 시)

```bash
# 1. 패널 빌드
cd Mobile-Panel && npm run build && cd ..

# 2. wwwroot 동기화
bash scripts/copy-panel.sh

# 3. 서버 publish
scripts\publish-server.bat

# 4. UE 패키징 (에디터에서)
# Platforms → Windows → Package Project → Build/Windows/
```

---

## 데모용 노트북 배포

`Build/` 폴더 전체를 복사하면 됩니다:

```
Build/
├── Windows/     ← UE 게임 (ExhibitionClient.exe)
└── publish/     ← 서버 + 패널
```

노트북에서 `Build/Windows/ExhibitionClient.exe` 실행하면:
1. UE 씬 시작
2. `ExhibitionServerLauncher`가 `Build/publish/ExhibitionServer.exe` 자동 실행
3. `http://[노트북 IP]:5225` 에서 모바일 패널 서빙

폰/태블릿에서 `http://[노트북 IP]:5225` 접속 → 패널 → 캐릭터 조종

> **Windows 방화벽:** 첫 실행 시 포트 5225 허용 팝업 → 허용 필요

---

## 자주 하는 실수

| 실수 | 결과 | 해결 |
|------|------|------|
| `npm run build` 후 `publish-server.bat` 바로 실행 | wwwroot에 패널 파일 없음 | `copy-panel.sh` 먼저 실행 |
| UE 패키징 후 서버 재publish | 불필요한 작업 | UE는 독립적, 서버 건드릴 필요 없음 |
| 서버 변경 후 UE 재패키징 | 불필요한 작업 (20~30분 낭비) | 서버만 `publish-server.bat` 실행 |
| `Build/publish/` 없이 실행 | 서버 못 찾아서 SignalR 연결 안됨 | `publish-server.bat` 실행 |

---

## 포트 정리

| 포트 | 용도 |
|------|------|
| 5225 | ASP.NET 서버 (패널 서빙 + SignalR + WebSocket) |
| 3001 | Vite 개발 서버 (개발 환경 only, 배포 시 불필요) |
---

## 2026-05-06 최신 수동 배포 절차: itch.io / Windows ZIP 기준

현재 Windows 패키징 구조는 서버를 `Build/publish`에서 찾는 방식이 아니라, 게임 exe 옆의 `ExhibitionServer` 폴더에서 찾는 방식으로 정리한다.

최종 폴더 구조:

```text
Build/Windows/
  ExhibitionClient.exe
  ExhibitionClient/
  Engine/
  ExhibitionServer/
    ExhibitionServer.exe
    appsettings.json
    appsettings.Production.json
    wwwroot/
      index.html
      assets/
    Data/
      ExhibitionKnowledge/
```

Unreal 설정:

```ini
[/Script/ExhibitionClient.ExhibitionServerLauncher]
ServerExePath=ExhibitionServer/ExhibitionServer.exe
bAutoLaunch=true
```

### 각 명령의 역할

```text
npm run build
= React/Vite 모바일 패널을 브라우저가 읽을 수 있는 정적 파일로 포장한다.
= 결과는 Mobile-Panel/dist 에 생긴다.
```

```text
dist -> wwwroot 복사
= ASP.NET 서버가 모바일 패널을 직접 서빙할 수 있게 한다.
= http://PC_IP:5225 로 접속하면 wwwroot의 index.html이 열린다.
```

```text
dotnet publish -c Release --runtime win-x64 --self-contained true
= ASP.NET 서버를 다른 Windows PC에서도 실행 가능한 배포용 폴더로 만든다.
= self-contained true 이므로 대상 PC에 .NET 설치가 없어도 실행 가능하다.
```

```text
Build/publish -> Build/Windows/ExhibitionServer 복사
= Unreal exe가 자동 실행할 서버를 게임 폴더 안에 넣는다.
```

### 권장 순서

Unreal 패키징은 `Build/Windows`를 덮어쓸 수 있다. 따라서 서버/패널은 Unreal 패키징 후에 반영하는 것이 안전하다.

```text
1. Unreal Editor에서 Windows 패키징
2. Mobile-Panel npm run build
3. Mobile-Panel/dist -> Server-AspNet/ExhibitionServer/wwwroot 복사
4. dotnet publish로 서버 배포 폴더 생성
5. Build/Windows/ExhibitionServer를 publish 결과로 교체
6. Build/Windows 실행 테스트
7. Build/Windows 내부 파일을 zip으로 압축
8. itch.io에 zip 업로드
```

### PowerShell 수동 명령

루트 기준:

```powershell
cd C:\Portfolio\InteractiveExhibition
```

모바일 패널 build:

```powershell
cd .\Mobile-Panel
npm run build
cd ..
```

패널 dist를 서버 wwwroot로 복사:

```powershell
Remove-Item -Recurse -Force .\Server-AspNet\ExhibitionServer\wwwroot\*
Copy-Item -Recurse .\Mobile-Panel\dist\* .\Server-AspNet\ExhibitionServer\wwwroot\
```

서버 publish:

```powershell
dotnet publish .\Server-AspNet\ExhibitionServer\ExhibitionServer.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output .\Build\publish
```

publish 결과를 최종 Windows 패키지에 반영:

```powershell
Remove-Item -Recurse -Force .\Build\Windows\ExhibitionServer
New-Item -ItemType Directory -Path .\Build\Windows\ExhibitionServer
Copy-Item -Recurse .\Build\publish\* .\Build\Windows\ExhibitionServer\
```

### 테스트

서버 단독 확인:

```powershell
cd C:\Portfolio\InteractiveExhibition\Build\Windows\ExhibitionServer
.\ExhibitionServer.exe
```

브라우저에서 확인:

```text
http://127.0.0.1:5225
http://127.0.0.1:5225/api/panel/qr.svg
```

Unreal 포함 확인:

```powershell
cd C:\Portfolio\InteractiveExhibition\Build\Windows
.\ExhibitionClient.exe
```

확인 항목:

```text
1. ExhibitionServer.exe가 자동 실행되는지
2. QR이 표시되는지
3. 휴대폰에서 http://PC_IP:5225 접속되는지
4. AI 채팅이 Azure Gateway를 통해 응답하는지
5. 캐릭터가 중복 생성되지 않는지
```

### itch.io ZIP 주의

zip 안에 바로 실행 파일이 보이는 구조가 좋다.

좋은 구조:

```text
ExhibitionClient.exe
ExhibitionClient/
Engine/
ExhibitionServer/
```

피할 구조:

```text
Build/Windows/ExhibitionClient.exe
```

즉, `Build/Windows` 폴더 자체를 압축하되 zip 내부에 `Build/Windows` 상위 폴더가 한 번 더 들어가지 않게 압축한다.

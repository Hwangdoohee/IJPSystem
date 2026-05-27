# IJP System 배포 가이드

현장 PC 에 설치할 수 있는 `IJPSystem_Setup_x.x.x.exe` 인스톨러를 만드는 절차입니다.

---

## 한 줄 요약

```powershell
.\build\publish.ps1          # 1. 빌드 + 런타임 포함 패키징
.\build\make-installer.ps1   # 2. Inno Setup 으로 setup.exe 묶기
```

산출물: `build\Output\IJPSystem_Setup_1.0.0.exe` (약 60~80 MB 예상)

---

## 사전 준비 (개발 PC 1회만)

1. **.NET 8 SDK** — 이미 설치되어 있음 (`dotnet --version` 으로 확인)
2. **Inno Setup 6** — https://jrsoftware.org/isdl.php  에서 `innosetup-6.x.x.exe` 다운로드 후 설치
   - 기본 경로 (`C:\Program Files (x86)\Inno Setup 6\`) 에 설치하면 스크립트가 자동으로 찾음

---

## 단계별 설명

### 1단계 — `publish.ps1`

`dotnet publish` 를 다음 옵션으로 실행:

| 옵션 | 의미 |
|---|---|
| `-c Release` | Release 모드 빌드 (최적화 ON) |
| `-r win-x64` | Windows 64bit 타겟 |
| `--self-contained true` | .NET 8 런타임을 결과물에 포함 → **현장 PC 에 .NET 설치 불필요** |
| `PublishReadyToRun=true` | AOT 사전컴파일 → 초기 기동 시간 단축 |
| `PublishSingleFile=false` | WPF 는 단일파일 시 XAML/Resource 로딩 이슈 가능 → false 유지 |

결과: `build\publish\IJPSystem\` 폴더에 약 **191 MB** (런타임 포함).
- `IJPSystem.Platform.HMI.exe` (메인 실행파일)
- 약 200여 개 DLL (.NET 8 런타임 + 프로젝트 + NuGet)
- `Config\` (RecipeData.db, AlarmSystem.db, IO.json, VisionConfig.json …)

### 2단계 — `make-installer.ps1`

Inno Setup 컴파일러 `ISCC.exe` 가 `installer.iss` 를 읽어 `setup.exe` 한 개로 묶음.

`installer.iss` 주요 설정:

| 항목 | 값 | 비고 |
|---|---|---|
| 설치 경로 | `C:\IJPSystem\` | **`Program Files` 사용 안 함** — SQLite DB 쓰기 권한 문제 회피 |
| 시작 메뉴 그룹 | `IJP System` | 자동 생성 |
| 바탕화면 아이콘 | 선택 (기본 OFF) | 설치 마법사에서 체크박스 |
| Config DB 파일 | `onlyifdoesntexist` | **재설치 시 운영 레시피/알람 데이터 보존** |
| `C:\Logs\` 폴더 | 자동 생성 | NozzleHealth.db / 시스템 로그 저장 위치 |
| 언어 | 한국어 / 영어 | 설치 마법사 다국어 |
| 압축 | `lzma2/ultra` | 191 MB → 약 60~80 MB 로 압축 |

---

## 버전 올리는 법

1. `installer.iss` 의 `#define MyAppVersion "1.0.0"` 수정
2. `IJPSystem.Platform.HMI\AssemblyInfo.cs` 의 어셈블리 버전도 같이 올리는 것을 권장

---

## 현장 설치 절차

1. `IJPSystem_Setup_1.0.0.exe` 를 현장 PC 로 복사 (USB / 네트워크)
2. 더블클릭 → 관리자 권한 승인 → 설치 마법사 진행
3. 설치 완료 후 바탕화면 아이콘 또는 시작 메뉴에서 실행

### 언인스톨

제어판 > 프로그램 추가/제거 > **IJP System** 에서 제거.
- 운영 DB (`Config\*.db`) 는 보존됨 — 완전 삭제하려면 `C:\IJPSystem\` 폴더 수동 삭제

---

## ACS / Comizoa 모션 DLL 추가 시 (추후)

현재 `IJPSystem.Drivers.Motion\lib\ACS\` 는 비어 있음.
DLL 을 채워넣고 csproj 의 `<Reference>` 활성화 후 다시 `publish.ps1` 실행하면,
`build\publish\IJPSystem\` 산출물에 자동 포함되어 인스톨러에도 같이 묶임.

**32bit/64bit 주의** — ACS SPiiPlusNET 은 비트 매칭 필요. csproj 가 `win-x64` 면 ACS 도 x64 DLL 사용.

---

## 트러블슈팅

| 증상 | 원인 / 해결 |
|---|---|
| `dotnet publish` 가 실패 | `dotnet build IJPSystem.slnx` 가 먼저 통과하는지 확인 |
| `ISCC.exe` 를 못 찾음 | Inno Setup 6 가 기본 경로에 설치되었는지 확인 |
| 설치 후 실행하면 즉시 종료 | 현장 PC 의 Windows Defender / 백신이 차단 — 예외 등록 또는 디지털 서명 |
| 레시피 화면이 비어있음 | `Config\RecipeData.db` 가 복사되지 않음 — `build\publish\IJPSystem\Config\` 확인 |
| 폰트 깨짐 | Malgun Gothic / Segoe UI 가 현장 PC 에 있는지 확인 (Windows 기본 폰트라 보통 OK) |

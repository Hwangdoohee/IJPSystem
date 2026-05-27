; IJPSystem HMI 인스톨러 정의 (Inno Setup 6)
; 컴파일: ISCC.exe installer.iss   또는  Inno Setup Compiler GUI 에서 열기
; 산출물: build\Output\IJPSystem_Setup_{버전}.exe

#define MyAppName       "IJP System"
#define MyAppVersion    "1.0.0"
#define MyAppPublisher  "IJP"
#define MyAppExeName    "IJPSystem.Platform.HMI.exe"
#define SrcDir          "publish\IJPSystem"

[Setup]
AppId={{D2C5F1A6-3F4B-4B5E-9A2C-7E8D9B0A1C2E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=C:\IJPSystem
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=IJPSystem_Setup_{#MyAppVersion}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
; 설치 후 C:\IJPSystem\ 폴더 전체에 Users 그룹 쓰기권한 부여
; — Config\*.db 에 운영 데이터 INSERT/UPDATE 가능하도록
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes

[Languages]
Name: "korean";  MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 실행파일 + .NET 8 런타임 + DLL 일괄 복사
Source: "{#SrcDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Config 폴더의 DB / JSON 파일은 onlyifdoesntexist 로 — 운영 중 누적된 레시피/알람 데이터 보존
Source: "{#SrcDir}\Config\*.db";   DestDir: "{app}\Config"; Flags: onlyifdoesntexist
Source: "{#SrcDir}\Config\*.json"; DestDir: "{app}\Config"; Flags: onlyifdoesntexist

[Dirs]
; 노즐 헬스 DB / 시스템 로그 저장 위치 — 실행 시 생성되지만 미리 만들고 모든 사용자에 쓰기 권한 부여
Name: "C:\Logs"; Permissions: users-modify
; Config 폴더 — SQLite DB 쓰기 권한 필요
Name: "{app}\Config"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}";        Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; 언인스톨 시 로그/캐시는 지우지만 운영 DB 는 남김 — 운영자가 직접 삭제하도록
Type: filesandordirs; Name: "{app}\logs"

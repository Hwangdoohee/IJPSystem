# ACS Motion Control SDK — 로컬 DLL 배치 위치

`AcsMotionDriver` 가 실제 SPiiPlus 컨트롤러와 통신하려면 ACS SDK DLL 이 필요하다.
지금은 DLL 미배치 상태이며 드라이버 본문은 placeholder 만 갖고 있어 빌드는 통과한다.

## 받아서 여기에 넣을 DLL (3개)

| 파일 | 역할 |
|---|---|
| `ACS.SPiiPlusNET.dll` | 관리형 .NET 래퍼 (`using ACS.SPiiPlusNET;` 의 `Api` 클래스) |
| `ACSCInterop.dll` | 네이티브 SDK ↔ .NET 브리지 |
| `ACSC.dll` | 네이티브 C SDK (실제 통신 — x86/x64 별도) |

획득 경로 — ACS 공식 사이트에서 **ACS-Tools (MMI 패키지)** 설치, 또는 회사 장비 PC 에서 복사:
```
C:\Program Files (x86)\ACS Motion Control\ACS-Tools\Bin\Dotnet\ACS.SPiiPlusNET.dll
C:\Program Files (x86)\ACS Motion Control\ACS-Tools\Bin\ACSCInterop.dll
C:\Program Files (x86)\ACS Motion Control\ACS-Tools\Bin\ACSC.dll
```

세 파일을 이 폴더(`IJPSystem.Drivers.Motion/lib/ACS/`)에 그대로 복사.

## DLL 배치 후 csproj 에 참조 추가

`IJPSystem.Drivers.Motion.csproj` 에 아래 블록을 추가하면 활성화된다:

```xml
<ItemGroup>
  <Reference Include="ACS.SPiiPlusNET">
    <HintPath>lib\ACS\ACS.SPiiPlusNET.dll</HintPath>
    <Private>true</Private>
  </Reference>
</ItemGroup>

<ItemGroup>
  <None Include="lib\ACS\ACSCInterop.dll" CopyToOutputDirectory="PreserveNewest"/>
  <None Include="lib\ACS\ACSC.dll"        CopyToOutputDirectory="PreserveNewest"/>
</ItemGroup>
```

## 다음 단계

1. 위 참조 추가 후 빌드 통과 확인
2. `AcsMotionDriver.cs` 의 `// TODO:` 위치를 실제 SDK 호출로 채움
   - `_api.OpenCommEthernet(ip, port)` 또는 `OpenCommSimulator`
   - `_api.Enable / Disable / ToPoint / Jog / Halt / FaultClear` 등
3. `MotorConfig.json` 또는 별도 설정에서 어떤 드라이버 (Virtual / ACS / Comizoa) 를 쓸지 분기 처리

## 플랫폼 일치 주의

`ACSC.dll` 은 x86 / x64 별도 빌드. csproj 의 `PlatformTarget` 과 일치해야 한다.
- HMI 가 `x64` 이면 → x64 `ACSC.dll`
- `AnyCPU + Prefer32Bit` → x86 `ACSC.dll`

## 라이선스

DLL 자체엔 별도 라이선스 X — SPiiPlus 컨트롤러에 내장. 시뮬레이터도 무료.

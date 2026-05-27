# IJPSystem HMI 배포용 publish 스크립트
# 사용법: PowerShell 에서  .\build\publish.ps1
# 결과: build\publish\IJPSystem\ 폴더에 실행파일 + 런타임 + Config 가 만들어짐

$ErrorActionPreference = 'Stop'
$root      = Split-Path -Parent $PSScriptRoot
$proj      = Join-Path $root 'IJPSystem.Platform.HMI\IJPSystem.Platform.HMI.csproj'
$outDir    = Join-Path $PSScriptRoot 'publish\IJPSystem'
$configSrc = Join-Path $root 'Config'

Write-Host "==> [1/3] Clean previous publish ..." -ForegroundColor Cyan
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

Write-Host "==> [2/3] dotnet publish (self-contained, win-x64) ..." -ForegroundColor Cyan
dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $outDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit=$LASTEXITCODE)" }

Write-Host "==> [3/3] Copy Config\ (Recipe / Alarm / IO / Vision / AppConfig) ..." -ForegroundColor Cyan
$cfgDst = Join-Path $outDir 'Config'
if (-not (Test-Path $cfgDst)) { New-Item -ItemType Directory -Path $cfgDst | Out-Null }
Copy-Item -Path (Join-Path $configSrc '*') -Destination $cfgDst -Recurse -Force

$size = [Math]::Round(((Get-ChildItem $outDir -Recurse | Measure-Object Length -Sum).Sum / 1MB), 1)
Write-Host ""
Write-Host "Done!  Output: $outDir  ($size MB)" -ForegroundColor Green
Write-Host "Next : .\build\make-installer.ps1   (Inno Setup 으로 setup.exe 생성)" -ForegroundColor Yellow

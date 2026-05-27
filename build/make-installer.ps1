# Inno Setup 컴파일러 호출 → setup.exe 생성
# 사전조건: publish.ps1 먼저 실행, Inno Setup 6 설치 (https://jrsoftware.org/isdl.php)

$ErrorActionPreference = 'Stop'
$iss = Join-Path $PSScriptRoot 'installer.iss'

$iscc = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) {
    Write-Host "Inno Setup 6 이 설치되어 있지 않습니다." -ForegroundColor Red
    Write-Host "https://jrsoftware.org/isdl.php  에서 innosetup-6.x.exe 다운로드 후 설치하세요." -ForegroundColor Yellow
    exit 1
}

Push-Location $PSScriptRoot
try {
    & $iscc $iss
    if ($LASTEXITCODE -ne 0) { throw "ISCC failed (exit=$LASTEXITCODE)" }
    $out = Join-Path $PSScriptRoot 'Output'
    Write-Host ""
    Write-Host "Setup created in: $out" -ForegroundColor Green
    Get-ChildItem $out -Filter '*.exe' | ForEach-Object {
        $mb = [Math]::Round($_.Length / 1MB, 1)
        Write-Host "  $($_.Name)  ($mb MB)"
    }
}
finally { Pop-Location }

param(
    [string]$Version = "2.0.0",
    [string]$SupervisorPin = "",
    [switch]$SkipPayload,
    [switch]$SkipInstaller,
    [string]$PfxPath = "",
    [string]$PfxPassword = "",
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"
$Version = $Version.TrimStart("v", "V")
$nativeRoot = Split-Path -Parent $PSScriptRoot
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "dotnet SDK not found."
    }
    $dotnet = $command.Source
}

$nugetConfig = Join-Path $nativeRoot "NuGet.config"
$buildRoot = Join-Path $nativeRoot "build"
$payloadRoot = Join-Path $nativeRoot "installer\payload"
$solution = Join-Path $nativeRoot "Ara.Desktop.sln"
$testProject = Join-Path $nativeRoot "tests\Ara.Tests\Ara.Tests.csproj"
$appProject = Join-Path $nativeRoot "src\Ara.Desktop\Ara.Desktop.csproj"
$installerScript = Join-Path $nativeRoot "installer\inno_setup.iss"

if ($SupervisorPin) {
    $env:SUPERVISOR_PIN = $SupervisorPin
}

Write-Host "ARA Assessment build $Version" -ForegroundColor Cyan
Write-Host "Native root: $nativeRoot"
Write-Host "Supervisor PIN configured: $([bool]$env:SUPERVISOR_PIN)"

if (Test-Path $buildRoot) {
    Remove-Item $buildRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null

& $dotnet restore $solution --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed."
}

Write-Host "Running tests..." -ForegroundColor Yellow
& $dotnet test $testProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Unit tests failed."
}

foreach ($platform in @("x64", "x86")) {
    $output = Join-Path $buildRoot "app-$platform"
    Write-Host "Building app $platform..." -ForegroundColor Yellow
    & $dotnet build $appProject `
        -c Release `
        -p:Platform=$platform `
        -p:PlatformTarget=$platform `
        -p:Version=$Version `
        --configfile $nugetConfig `
        -o $output
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $platform."
    }
}

if (-not $SkipPayload) {
    & (Join-Path $PSScriptRoot "download-payload.ps1") -PayloadRoot $payloadRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Prerequisite payload download failed."
    }
}

foreach ($platform in @("x64", "x86")) {
    $source = Join-Path $buildRoot "app-$platform"
    $destination = Join-Path $payloadRoot "app-$platform"
    if (Test-Path $destination) {
        Remove-Item $destination -Recurse -Force
    }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item (Join-Path $source "*") $destination -Recurse -Force
}

$appFiles = @(
    (Join-Path $payloadRoot "app-x64\ARA Assessment.exe"),
    (Join-Path $payloadRoot "app-x64\Ara.Core.dll"),
    (Join-Path $payloadRoot "app-x86\ARA Assessment.exe"),
    (Join-Path $payloadRoot "app-x86\Ara.Core.dll")
)

$signingPfx = $PfxPath
$signingPassword = $PfxPassword
$temporaryPfx = $false
if (-not $signingPfx -and $env:SIGNING_CERT_BASE64) {
    $signingPfx = Join-Path $env:TEMP "ara-release-signing.pfx"
    [IO.File]::WriteAllBytes($signingPfx, [Convert]::FromBase64String($env:SIGNING_CERT_BASE64))
    $signingPassword = $env:SIGNING_CERT_PASSWORD
    $temporaryPfx = $true
}

try {
    if ($signingPfx) {
        & (Join-Path $PSScriptRoot "sign.ps1") `
            -Files $appFiles `
            -PfxPath $signingPfx `
            -PfxPassword $signingPassword `
            -TimestampUrl $TimestampUrl
        if ($LASTEXITCODE -ne 0) {
            throw "Application signing failed."
        }
    }
    else {
        Write-Warning "No signing certificate. Application binaries are unsigned."
    }

    if ($SkipInstaller) {
        Write-Host "Installer build skipped." -ForegroundColor Yellow
        return
    }

    $iscc = $null
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        $iscc = $command.Source
    }

    if (-not $iscc) {
        $candidates = @(
            (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
            (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
            (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
        )
        foreach ($candidate in $candidates) {
            if (Test-Path $candidate) {
                $iscc = $candidate
                break
            }
        }
    }

    if (-not $iscc) {
        throw "Inno Setup 6 ISCC.exe was not found."
    }

    Write-Host "Compiling installer..." -ForegroundColor Yellow
    & $iscc "/DMyAppVersion=$Version" $installerScript
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed."
    }

    $installer = Join-Path $buildRoot "installer\ARA_Assessment_Setup_v$Version.exe"
    if (-not (Test-Path $installer)) {
        throw "Installer output not found: $installer"
    }

    if ($signingPfx) {
        & (Join-Path $PSScriptRoot "sign.ps1") `
            -Files @($installer) `
            -PfxPath $signingPfx `
            -PfxPassword $signingPassword `
            -TimestampUrl $TimestampUrl
        if ($LASTEXITCODE -ne 0) {
            throw "Installer signing failed."
        }
    }

    $hash = (Get-FileHash $installer -Algorithm SHA256).Hash
    $checksumPath = Join-Path $buildRoot "installer\SHA256SUMS.txt"
    "$hash  $(Split-Path -Leaf $installer)" | Set-Content -Path $checksumPath -Encoding ASCII

    Write-Host ""
    Write-Host "Build complete." -ForegroundColor Green
    Write-Host "Installer: $installer"
    Write-Host "SHA256:    $hash"
}
finally {
    if ($temporaryPfx -and (Test-Path $signingPfx)) {
        Remove-Item $signingPfx -Force -ErrorAction SilentlyContinue
    }
}

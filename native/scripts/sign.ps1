param(
    [string[]]$Files = @(),
    [string]$PfxPath = "",
    [string]$PfxPassword = "",
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

if (-not $PfxPath) {
    Write-Warning "No signing certificate supplied. Binaries will remain unsigned."
    return
}

if (-not (Test-Path $PfxPath)) {
    throw "Signing certificate not found: $PfxPath"
}

if (-not $Files -or $Files.Count -eq 0) {
    return
}

$signTool = $null
$command = Get-Command signtool.exe -ErrorAction SilentlyContinue
if ($command) {
    $signTool = $command.Source
}

if (-not $signTool) {
    $roots = @(
        (Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"),
        (Join-Path $env:ProgramFiles "Windows Kits\10\bin")
    )

    foreach ($root in $roots) {
        if (-not (Test-Path $root)) {
            continue
        }

        $candidate = Get-ChildItem $root -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match "\\x64\\" } |
            Select-Object -First 1
        if ($candidate) {
            $signTool = $candidate.FullName
            break
        }
    }
}

if (-not $signTool) {
    throw "signtool.exe was not found. Install the Windows SDK or add signtool to PATH."
}

foreach ($file in $Files) {
    if (-not (Test-Path $file)) {
        throw "Signing target not found: $file"
    }

    $arguments = @(
        "sign",
        "/fd", "SHA256",
        "/tr", $TimestampUrl,
        "/td", "SHA256",
        "/f", $PfxPath,
        "/p", $PfxPassword,
        $file
    )

    & $signTool @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "signtool failed for $file with exit code $LASTEXITCODE"
    }
    Write-Host "SIGN $file"
}

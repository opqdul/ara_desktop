param(
    [string]$PayloadRoot = ""
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$nativeRoot = Split-Path -Parent $PSScriptRoot
if (-not $PayloadRoot) {
    $PayloadRoot = Join-Path $nativeRoot "installer\payload"
}
$prereq = Join-Path $PayloadRoot "prereq"
New-Item -ItemType Directory -Path $prereq -Force | Out-Null

function Test-MicrosoftSignature([string]$Path) {
    try {
        $signature = Get-AuthenticodeSignature -FilePath $Path
        return ($signature.Status -eq "Valid" -and
                $signature.SignerCertificate -ne $null -and
                $signature.SignerCertificate.Subject -match "Microsoft")
    }
    catch {
        return $false
    }
}

function Get-PayloadFile([string]$Name, [string]$Url, [string]$Sha256) {
    $destination = Join-Path $prereq $Name
    if (Test-Path $destination) {
        if ((Test-MicrosoftSignature $destination) -and
            (-not $Sha256 -or (Get-FileHash $destination -Algorithm SHA256).Hash -eq $Sha256)) {
            Write-Host "OK   $Name"
            return
        }
        Remove-Item $destination -Force
    }

    $partial = "$destination.partial"
    if (Test-Path $partial) {
        Remove-Item $partial -Force
    }

    Write-Host "GET  $Name"
    Invoke-WebRequest -Uri $Url -OutFile $partial -UseBasicParsing -MaximumRedirection 10 -TimeoutSec 900

    if ($Sha256) {
        $actual = (Get-FileHash $partial -Algorithm SHA256).Hash
        if ($actual -ne $Sha256) {
            Remove-Item $partial -Force
            throw "SHA256 mismatch for $Name. Expected $Sha256, got $actual."
        }
    }

    if (-not (Test-MicrosoftSignature $partial)) {
        Remove-Item $partial -Force
        throw "Microsoft signature verification failed for $Name."
    }

    Move-Item $partial $destination -Force
    Write-Host "OK   $Name"
}

Get-PayloadFile `
    "NDP462-KB3151900-x86-x64-AllOS-ENU.exe" `
    "https://go.microsoft.com/fwlink/?linkid=2099468" `
    "8550E370DB2400AEDB4397E9958F12041DEF3BB63C03CC7625CE07E09F42303E"

Get-PayloadFile `
    "ndp48-x86-x64-allos-enu.exe" `
    "https://go.microsoft.com/fwlink/?LinkId=2088631" `
    "0A3A390C47E639D0F7FC65B21195FEE6B7F65B066F80F70C60FAB191D14B7E40"

Get-PayloadFile `
    "VC_redist.x64.exe" `
    "https://aka.ms/vs/17/release/vc_redist.x64.exe" `
    "CC0FF0EB1DC3F5188AE6300FAEF32BF5BEEBA4BDD6E8E445A9184072096B713B"

Get-PayloadFile `
    "VC_redist.x86.exe" `
    "https://aka.ms/vs/17/release/vc_redist.x86.exe" `
    "0C09F2611660441084CE0DF425C51C11E147E6447963C3690F97E0B25C55ED64"

Get-PayloadFile `
    "MicrosoftEdgeStandaloneInstallerX64.exe" `
    "https://catalog.s.download.windowsupdate.com/c/msdownload/update/software/updt/2023/09/microsoftedgestandaloneinstallerx64_1c890b4b8dd6b7c93da98ebdc08ecdc5e30e50cb.exe" `
    "EAC95C8095EC5F9971EADE9827D8FB67FD251F5C16E702B5312D31067E39119B"

Get-PayloadFile `
    "MicrosoftEdgeStandaloneInstallerX86.exe" `
    "https://catalog.s.download.windowsupdate.com/c/msdownload/update/software/updt/2023/09/microsoftedgestandaloneinstallerx86_179f59bc54d73843d9288a9fd5609de0e507b911.exe" `
    "60B22C71128F10ABC5E92DD8FED90FF6CA55B8382BA01E48D87FAF1664746A31"

Get-PayloadFile `
    "MicrosoftEdgeWebView2Setup.exe" `
    "https://go.microsoft.com/fwlink/p/?LinkId=2124703" `
    ""

Write-Host ""
Write-Host "Prerequisite payload ready: $prereq"

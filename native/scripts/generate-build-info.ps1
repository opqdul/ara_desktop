param(
    [string]$Output = "",
    [string]$Pin = "",
    [string]$Version = "2.0.0"
)

$ErrorActionPreference = "Stop"

if (-not $Pin) {
    $Pin = $env:SUPERVISOR_PIN
}

$hash = ""
if ($Pin) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Pin)
        $digest = $sha.ComputeHash($bytes)
        $builder = New-Object System.Text.StringBuilder
        foreach ($value in $digest) {
            [void]$builder.Append($value.ToString("x2"))
        }
        $hash = $builder.ToString()
    }
    finally {
        $sha.Dispose()
    }
}

if (-not $Output) {
    $Output = Join-Path $PSScriptRoot "..\src\Ara.Core\obj\BuildInfo.g.cs"
}

$directory = Split-Path -Parent $Output
if ($directory -and -not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$content = @"
namespace Ara.Core
{
    internal static class BuildInfo
    {
        public const string SupervisorPinHash = "$hash";
        public const string AppVersion = "$Version";
    }
}
"@

Set-Content -Path $Output -Value $content -Encoding ASCII

# ARA Assessment — Native Windows Client

A native Windows rebuild of the ARA Assessment desktop client for Windows 7 SP1+
and old machines: .NET Framework 4.8 WinForms + WebView2, kiosk lockdown, an
offline installer, and GitHub Actions releases... but better.

The shipping Windows client lives in `native/`. The Flutter app at the repo root
is legacy/reference and is not part of the Windows release.

## Support matrix

| OS | Architecture | Runtime |
|---|---|---|
| Windows 7 SP1 (with SHA-2 and TLS 1.2 updates) | x86 / x64 | WebView2 109.0.1518.140 |
| Windows 8 / 8.1 | x86 / x64 | WebView2 109.0.1518.140 |
| Windows 10 LTSC 2015 / 2016 / 2019 / 2021 | x64 | Evergreen WebView2 |
| Windows 10 SAC 1709+ / 22H2 | x64 | Evergreen WebView2 |
| Windows 11 (all current releases) | x64 | Evergreen WebView2 |
| Windows 10 SAC 1511 / 1607 / 1703 | — | Not supported; installer refuses |

The app also runs on low-end GPUs. If D3D11 feature level is below 10_0, it
automatically asks WebView2 for software rendering.

## What it does

- Full-bleed WebView2 portal at `https://ara-assessment.com/assess`.
- Camera/microphone auto-grant only for `ara-assessment.com`.
- Normal and Exam modes. Exam mode maximizes the window, locks close/minimize/
  resize controls, blocks unauthorized foreground applications, allows Word,
  Excel, PowerPoint, WPS and LibreOffice, and reports violations to the portal.
- Supervisor panel at `Ctrl+Shift+F12` or `Alt+F12`, protected by a build-time
  PIN (3 attempts, 30-second lockout, audited actions).
- Office document links and downloads, sanitized filenames, and a 24-hour
  download cleanup.
- File logging, startup diagnostics, and a specific error screen for every
  failure class — never a blank window.
- Single instance, per-machine install, desktop shortcut checked by default,
  optional data deletion on uninstall.

## Install

1. Uninstall the old Flutter/vendor ARA Assessment v1.x first (Apps & Features).
   The installer refuses to coexist with it.
2. Download `ARA_Assessment_Setup_v2.0.1.exe` from GitHub Releases.
3. Verify it against `SHA256SUMS.txt`.
4. Run it as administrator. The installer checks the OS build, Windows 7
   SHA-2/servicing-stack updates, .NET 4.8, Visual C++ 2015–2022, WebView2
   (109 on Windows 7/8.1), free disk space, the old app, and a running instance
   before installing. It installs the offline prerequisites when needed and
   enables TLS 1.2 on legacy Windows.

The installer creates the Start Menu entry and checks **Create a desktop icon**
by default.

## Build

Build host: Windows 10/11 with the .NET 8 SDK, PowerShell, and Inno Setup 6.4.x.

```powershell
./native/scripts/build.ps1 -Version 2.0.1 -SupervisorPin "your-pin"
```

The script restores, runs the 56 unit tests, builds x64 and x86, downloads and
verifies the prerequisite payload, compiles the installer, signs when a
certificate is supplied, and writes `SHA256SUMS.txt`.

Output:

```
native/build/installer/ARA_Assessment_Setup_v2.0.1.exe
native/build/installer/SHA256SUMS.txt
```

Use `-SkipPayload` to reuse an already-downloaded payload.

## GitHub Actions release

`.github/workflows/native-windows-release.yml` builds, tests, signs, and
publishes the release from a `v*` tag. The release is created by GitHub Actions;
the commit and tag author remain the maintainer.

Add these repository secrets:

| Secret | Purpose |
|---|---|
| `SUPERVISOR_PIN` | Supervisor panel PIN. Hashed at build time; never shipped in plaintext. If omitted, the supervisor panel is disabled. |
| `SIGNING_CERT_BASE64` | Base64-encoded PFX for Authenticode signing. If omitted, binaries are unsigned. |
| `SIGNING_CERT_PASSWORD` | PFX password. |

Release:

```powershell
git tag v2.0.1
git push origin v2.0.1
```

The workflow uploads the installer + `SHA256SUMS.txt` as artifacts and attaches
them to the GitHub Release.

## Runtime data

| Data | Path |
|---|---|
| Logs | `%LOCALAPPDATA%\ARA Assessment\logs` |
| WebView2 cache | `%LOCALAPPDATA%\ARA Assessment\WebView2Native` |
| Downloads | `%USERPROFILE%\Documents\ARA Assessment\Downloads` |

Diagnostics:

```powershell
& "C:\Program Files\ARA Assessment\ARA Assessment.exe" --diagnostics --quiet
```

Smoke test (login page render proof, writes `smoke-result.json` to the log
folder):

```powershell
& "C:\Program Files\ARA Assessment\ARA Assessment.exe" --smoke-test
```

## Security notes

- TLS validation is never bypassed; mixed content is blocked.
- Dev flags are compile-time only; there is no editable `.env` in the shipped app.
- Camera/microphone permissions are origin-scoped to `ara-assessment.com`.
- Supervisor actions are audited; the panel requires the build-time PIN.
- The kiosk is a user-mode deterrent, not a security boundary. Use Windows
  policies or Assigned Access where a stronger boundary is required.

## Legacy Flutter builds

The Flutter code at the repository root is reference only. The
`legacy-flutter.yml` workflow builds it on macOS and Linux:

- macOS: `scripts/build_macos.sh` produces `build/macos/*.pkg` and `*.dmg`
  artifacts (`legacy-macos-flutter`).
- Linux: `flutter create --platforms=linux` + `flutter build linux` compiles the
  app, but the legacy `flutter_inappwebview` has no Linux native backend, so the
  app is expected to be non-functional (no webview) at runtime. The build log
  and bundle are uploaded as `legacy-linux-flutter-diagnostics`.

These are not production artifacts. The workflow publishes them to a separate
`legacy-flutter` prerelease; the production `v2.0.1` release contains only the
native Windows installer.

; ARA Assessment native Windows installer
; Target: Windows 7 SP1+ (x86/x64), Windows 8/8.1, Windows 10 1709+/LTSC, Windows 11.
; Build with Inno Setup 6.4.x.

#ifndef MyAppVersion
  #define MyAppVersion "2.0.0"
#endif

#define MyAppName "ARA Assessment"
#define MyAppPublisher "PT ARA Indonesia"
#define MyAppURL "https://ara-assessment.com"
#define MyAppExeName "ARA Assessment.exe"

[Setup]
AppId={{B7C0E3F4-6A1D-4F6E-9E35-9F2B1D8C7A11}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright (C) 2026 {#MyAppPublisher}. All rights reserved.
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Native Installer
VersionInfoTextVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}. All rights reserved.
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}.0
VersionInfoProductTextVersion={#MyAppVersion}

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=6.1sp1
CloseApplications=yes
RestartApplications=no
AppMutex=Local\ARAAssessmentDesktop
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/fast
SolidCompression=no
LZMAUseSeparateProcess=yes
OutputDir=..\build\installer
OutputBaseFilename=ARA_Assessment_Setup_v{#MyAppVersion}
SetupIconFile=..\src\Ara.Desktop\app.ico
WizardStyle=modern
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "payload\app-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,runtimes,Microsoft.Web.WebView2.Wpf.dll"; Check: Is64BitInstallMode
Source: "payload\app-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,runtimes,Microsoft.Web.WebView2.Wpf.dll"; Check: not Is64BitInstallMode

Source: "payload\prereq\ndp48-x86-x64-allos-enu.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsDotNet48
Source: "payload\prereq\VC_redist.x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsVcRedistX64
Source: "payload\prereq\VC_redist.x86.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsVcRedistX86
Source: "payload\prereq\MicrosoftEdgeStandaloneInstallerX64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsWebView2LegacyX64
Source: "payload\prereq\MicrosoftEdgeStandaloneInstallerX86.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsWebView2LegacyX86
Source: "payload\prereq\MicrosoftEdgeWebView2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsWebView2Modern

[Registry]
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client"; ValueType: dword; ValueName: "Enabled"; ValueData: "1"; Flags: createvalueifdoesntexist; Check: IsLegacyWindows
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.2\Client"; ValueType: dword; ValueName: "DisabledByDefault"; ValueData: "0"; Flags: createvalueifdoesntexist; Check: IsLegacyWindows
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\WinHttp"; ValueType: dword; ValueName: "DefaultSecureProtocols"; ValueData: "2048"; Flags: createvalueifdoesntexist; Check: IsLegacyWindows
Root: HKLM; Subkey: "SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Internet Settings\WinHttp"; ValueType: dword; ValueName: "DefaultSecureProtocols"; ValueData: "2048"; Flags: createvalueifdoesntexist; Check: IsLegacyWindows
Root: HKLM; Subkey: "SOFTWARE\Microsoft\.NETFramework\v4.0.30319"; ValueType: dword; ValueName: "SchUseStrongCrypto"; ValueData: "1"; Flags: createvalueifdoesntexist; Check: IsLegacyWindows
Root: HKLM; Subkey: "SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v4.0.30319"; ValueType: dword; ValueName: "SchUseStrongCrypto"; ValueData: "1"; Flags: createvalueifdoesntexist; Check: IsLegacyWindows

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\ndp48-x86-x64-allos-enu.exe"; Parameters: "/q /norestart"; StatusMsg: "Memasang .NET Framework 4.8..."; Flags: waituntilterminated; Check: NeedsDotNet48
Filename: "{tmp}\VC_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Memasang Visual C++ Runtime x64..."; Flags: waituntilterminated; Check: NeedsVcRedistX64
Filename: "{tmp}\VC_redist.x86.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Memasang Visual C++ Runtime x86..."; Flags: waituntilterminated; Check: NeedsVcRedistX86
Filename: "{tmp}\MicrosoftEdgeStandaloneInstallerX64.exe"; Parameters: "/installsource windowsupdate /install ""appguid={{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}&needsadmin=true"""; StatusMsg: "Memasang WebView2 Runtime 109 (x64)..."; Flags: waituntilterminated; Check: NeedsWebView2LegacyX64
Filename: "{tmp}\MicrosoftEdgeStandaloneInstallerX86.exe"; Parameters: "/installsource windowsupdate /install ""appguid={{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}&needsadmin=true"""; StatusMsg: "Memasang WebView2 Runtime 109 (x86)..."; Flags: waituntilterminated; Check: NeedsWebView2LegacyX86
Filename: "{tmp}\MicrosoftEdgeWebView2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Memasang Microsoft Edge WebView2 Runtime..."; Flags: waituntilterminated; Check: NeedsWebView2Modern
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: PrerequisitesOk

[Code]
var
  OSVersion: TWindowsVersion;
  ShouldDeleteData: Boolean;

function IsLtscEdition(): Boolean;
var
  Edition, Product: String;
begin
  Result := False;
  Edition := '';
  Product := '';
  RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows NT\CurrentVersion', 'EditionID', Edition);
  RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows NT\CurrentVersion', 'ProductName', Product);
  Edition := Uppercase(Edition);
  Product := Uppercase(Product);
  Result := (Pos('ENTERPRISES', Edition) > 0) or
            (Pos('LTSB', Product) > 0) or
            (Pos('LTSC', Product) > 0);
end;

function IsLegacyWindows(): Boolean;
begin
  Result := OSVersion.Major < 10;
end;

function HasCbsPackage(const Needle: String): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
begin
  Result := False;
  if RegGetSubkeyNames(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages', Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
    begin
      if Pos(Uppercase(Needle), Uppercase(Names[I])) > 0 then
      begin
        Result := True;
        Exit;
      end;
    end;
  end;
end;

function CheckOs(): Boolean;
begin
  Result := False;
  GetWindowsVersionEx(OSVersion);

  if (OSVersion.Major = 6) and (OSVersion.Minor = 1) then
  begin
    if OSVersion.ServicePackMajor < 1 then
    begin
      MsgBox('Windows 7 memerlukan Service Pack 1.', mbError, MB_OK);
      Exit;
    end;
    if (not HasCbsPackage('KB4490628')) or (not HasCbsPackage('KB4474419')) then
    begin
      MsgBox('Windows 7 memerlukan update KB4490628 (servicing stack) dan KB4474419 (SHA-2).' + #13#10 +
             'Jalankan Windows Update atau instal kedua MSU tersebut, reboot, lalu jalankan installer ini lagi.',
             mbError, MB_OK);
      Exit;
    end;
    Result := True;
    Exit;
  end;

  if (OSVersion.Major = 6) and ((OSVersion.Minor = 2) or (OSVersion.Minor = 3)) then
  begin
    Result := True;
    Exit;
  end;

  if (OSVersion.Major = 10) and (OSVersion.Build >= 22000) then
  begin
    Result := True;
    Exit;
  end;

  if (OSVersion.Major = 10) then
  begin
    if (OSVersion.Build >= 16299) or IsLtscEdition() then
    begin
      Result := True;
      Exit;
    end;

    MsgBox('Windows 10 versi 1511/1607/1703 tidak didukung oleh WebView2.' + #13#10 +
           'Gunakan Windows 10 1709 atau lebih baru, atau Windows 10 LTSC 2015/2016/2019/2021.',
           mbError, MB_OK);
    Exit;
  end;

  MsgBox('Windows ini tidak didukung. ARA Assessment memerlukan Windows 7 SP1 atau lebih baru.',
         mbError, MB_OK);
end;

function CheckOldApp(): Boolean;
var
  Roots: array[0..2] of Integer;
  Names: TArrayOfString;
  I, R: Integer;
  SubKey, DisplayName, DisplayVersion: String;
begin
  Result := True;
  Roots[0] := HKCU;
  Roots[1] := HKLM;
  Roots[2] := HKLM64;

  for R := 0 to 2 do
  begin
    if RegGetSubkeyNames(Roots[R], 'Software\Microsoft\Windows\CurrentVersion\Uninstall', Names) then
    begin
      for I := 0 to GetArrayLength(Names) - 1 do
      begin
        SubKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + Names[I];
        DisplayName := '';
        DisplayVersion := '';
        if RegQueryStringValue(Roots[R], SubKey, 'DisplayName', DisplayName) and
           RegQueryStringValue(Roots[R], SubKey, 'DisplayVersion', DisplayVersion) then
        begin
          if (Pos('ARA ASSESSMENT', Uppercase(DisplayName)) > 0) and
             (Copy(DisplayVersion, 1, 2) = '1.') then
          begin
            MsgBox('Versi lama ARA Assessment (v' + DisplayVersion + ') terdeteksi.' + #13#10 +
                   'Uninstall versi lama melalui Apps & Features terlebih dahulu, lalu jalankan installer ini lagi.',
                   mbError, MB_OK);
            Result := False;
            Exit;
          end;
        end;
      end;
    end;
  end;
end;

function CheckRunningApp(): Boolean;
begin
  Result := True;
  if FindWindowByWindowName('ARA Assessment') <> 0 then
  begin
    MsgBox('ARA Assessment sedang berjalan.' + #13#10 +
           'Tutup aplikasi melalui panel pengawas atau tombol Close sebelum melanjutkan.',
           mbError, MB_OK);
    Result := False;
  end;
end;

function CheckDiskSpace(): Boolean;
var
  FreeMB, TotalMB: Cardinal;
begin
  Result := False;
  if not GetSpaceOnDisk(ExpandConstant('{autopf}'), True, FreeMB, TotalMB) then
  begin
    MsgBox('Tidak dapat memeriksa ruang disk.', mbError, MB_OK);
    Exit;
  end;

  if FreeMB < 4096 then
  begin
    MsgBox('Ruang disk tidak cukup. Diperlukan minimal 4 GB ruang kosong.', mbError, MB_OK);
    Exit;
  end;

  Result := True;
end;

function GetWebView2Version(): String;
var
  Value: String;
begin
  Result := '';
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value) then
  begin
    Result := Value;
    Exit;
  end;

  if RegQueryStringValue(HKLM64, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value) then
  begin
    Result := Value;
    Exit;
  end;

  if RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Value) then
  begin
    Result := Value;
  end;
end;

function IsWebView2LegacyVersion(const Value: String): Boolean;
begin
  Result := Pos('109.', Value) = 1;
end;

function NeedsDotNet48(): Boolean;
var
  Release: Cardinal;
begin
  Result := not (RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and
                 (Release >= 528040));
end;

function NeedsVcRedistX64(): Boolean;
var
  Installed: Cardinal;
begin
  Result := False;
  if not Is64BitInstallMode then
  begin
    Exit;
  end;

  Result := not (RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) and
                 (Installed = 1));
end;

function NeedsVcRedistX86(): Boolean;
var
  Installed: Cardinal;
begin
  Result := False;
  if Is64BitInstallMode then
  begin
    Exit;
  end;

  Result := not (RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86', 'Installed', Installed) and
                 (Installed = 1));
end;

function NeedsWebView2LegacyX64(): Boolean;
begin
  Result := Is64BitInstallMode and
            (OSVersion.Major < 10) and
            (not IsWebView2LegacyVersion(GetWebView2Version()));
end;

function NeedsWebView2LegacyX86(): Boolean;
begin
  Result := (not Is64BitInstallMode) and
            (OSVersion.Major < 10) and
            (not IsWebView2LegacyVersion(GetWebView2Version()));
end;

function NeedsWebView2Modern(): Boolean;
begin
  Result := (OSVersion.Major >= 10) and (GetWebView2Version() = '');
end;

function PrerequisitesOk(): Boolean;
var
  Version: String;
begin
  Result := False;
  if NeedsDotNet48() then
  begin
    Exit;
  end;

  if Is64BitInstallMode then
  begin
    if NeedsVcRedistX64() then
    begin
      Exit;
    end;
  end
  else
  begin
    if NeedsVcRedistX86() then
    begin
      Exit;
    end;
  end;

  Version := GetWebView2Version();
  if Version = '' then
  begin
    Exit;
  end;

  if (OSVersion.Major < 10) and (not IsWebView2LegacyVersion(Version)) then
  begin
    Exit;
  end;

  Result := True;
end;

function InitializeSetup(): Boolean;
begin
  Result := False;
  if not CheckOs() then
  begin
    Exit;
  end;

  if not CheckOldApp() then
  begin
    Exit;
  end;

  if not CheckRunningApp() then
  begin
    Exit;
  end;

  if not CheckDiskSpace() then
  begin
    Exit;
  end;

  Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  ShouldDeleteData := MsgBox(
    'Hapus cache WebView2, log, dan dokumen yang diunduh?' + #13#10 +
    'Pilih No untuk mempertahankan data pengguna.',
    mbConfirmation,
    MB_YESNO) = IDYES;
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not PrerequisitesOk() then
    begin
      MsgBox('Instalasi selesai, tetapi salah satu prasyarat belum terpenuhi.' + #13#10 +
             'Periksa log instalasi atau jalankan installer ulang.',
             mbError, MB_OK);
    end
    else
    begin
      MsgBox('Instalasi selesai. Jika Windows meminta restart, restart terlebih dahulu sebelum menggunakan ARA Assessment.',
             mbInformation, MB_OK);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and ShouldDeleteData then
  begin
    DelTree(ExpandConstant('{localappdata}\ARA Assessment'), True, True, True);
    DelTree(ExpandConstant('{userdocs}\ARA Assessment'), True, True, True);
  end;
end;

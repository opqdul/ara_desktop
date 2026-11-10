using System;
using System.Collections.Generic;
using System.IO;

namespace Ara.Core
{
    public static class AppConfig
    {
        public const string AppName = "ARA Assessment";
        public static readonly string AppVersion = BuildInfo.AppVersion;
        public const string ProductionUrl = "https://ara-assessment.com/assess";
        public const string SupportUrl = "https://ara-assessment.com";

        public static readonly string SupervisorPinHash = BuildInfo.SupervisorPinHash;

        public static readonly string LocalAppData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ARA Assessment");

        public static readonly string LogDirectory = Path.Combine(LocalAppData, "logs");

        public static readonly string WebViewDataFolder = Path.Combine(LocalAppData, "WebView2Native");

        public static readonly string DownloadsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ARA Assessment",
            "Downloads");

        public static readonly HashSet<string> OfficeAllowlist = new HashSet<string>(
            new[]
            {
                "winword.exe",
                "excel.exe",
                "powerpnt.exe",
                "wps.exe",
                "et.exe",
                "wpp.exe",
                "soffice.exe",
                "soffice.bin"
            },
            StringComparer.OrdinalIgnoreCase);

        public static readonly HashSet<string> ShellProcesses = new HashSet<string>(
            new[]
            {
                "dwm.exe",
                "taskhostw.exe",
                "textinputhost.exe",
                "shellexperiencehost.exe",
                "searchhost.exe",
                "startmenuexperiencehost.exe",
                "ctfmon.exe",
                "sihost.exe",
                "runtimebroker.exe",
                "fontdrvhost.exe"
            },
            StringComparer.OrdinalIgnoreCase);

        public static readonly HashSet<string> ShellClasses = new HashSet<string>(
            new[]
            {
                "Shell_TrayWnd",
                "Shell_SecondaryTrayWnd",
                "Progman",
                "WorkerW",
                "Windows.UI.Core.CoreWindow",
                "TextInputHost",
                "StartMenuExperienceHost"
            },
            StringComparer.OrdinalIgnoreCase);

        public static readonly HashSet<string> EmergencyProcesses = new HashSet<string>(
            new[] { "taskmgr.exe" },
            StringComparer.OrdinalIgnoreCase);

        public static readonly string[] NormalRouteFragments =
        {
            "/login",
            "/register",
            "/dashboard",
            "/profile",
            "/user",
            "/biodata",
            "/verifikasi",
            "/verification",
            "/step",
            "/instruksi",
            "/instruction",
            "/jadwal",
            "/schedule",
            "/password",
            "/log",
            "/pelanggaran",
            "/account",
            "/home",
            "/selesai",
            "/finish"
        };

        public static readonly string[] ExamRouteFragments =
        {
            "/ujian",
            "/exam",
            "/soal",
            "/essay",
            "/esai",
            "/uraian",
            "/test",
            "/cbt",
            "/quiz",
            "/mulai",
            "/kerjakan",
            "/start",
            "/assessment"
        };

        public static bool IsLegacyRuntimeOlderThanTen(int majorVersion)
        {
            return majorVersion < 10;
        }
    }
}

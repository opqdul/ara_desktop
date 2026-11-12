using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;

namespace Ara.Core
{
    public sealed class CapabilityReport
    {
        public bool OsSupported { get; set; }
        public string OsDescription { get; set; }
        public string OsError { get; set; }
        public int OsMajor { get; set; }
        public int OsMinor { get; set; }
        public int OsBuild { get; set; }

        public bool DotNet48 { get; set; }
        public string DotNetRelease { get; set; }

        public bool WebView2Available { get; set; }
        public bool WebView2LegacyCompatible { get; set; }
        public string WebView2Version { get; set; }
        public string WebView2Error { get; set; }

        public int D3DFeatureLevel { get; set; }
        public bool SoftwareRenderingRecommended { get; set; }
        public string D3DDescription { get; set; }

        public string AppVersion { get; set; }
        public string LogDirectory { get; set; }

        public static CapabilityReport Collect()
        {
            var report = new CapabilityReport
            {
                AppVersion = AppConfig.AppVersion,
                LogDirectory = AppConfig.LogDirectory,
                WebView2Version = string.Empty,
                WebView2Error = string.Empty,
                OsError = string.Empty,
                DotNetRelease = "unknown"
            };

            var version = NativeMethods.GetWindowsVersion();
            report.OsMajor = version.Major;
            report.OsMinor = version.Minor;
            report.OsBuild = version.Build;

            var productName = ReadRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "ProductName",
                "Windows");
            var editionId = ReadRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "EditionID",
                string.Empty);
            var displayVersion = ReadRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "DisplayVersion",
                string.Empty);

            report.OsDescription = string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} (build {2}.{3}.{4}{5})",
                productName,
                string.IsNullOrWhiteSpace(displayVersion) ? editionId : displayVersion,
                version.Major,
                version.Minor,
                version.Build,
                version.ServicePackMajor > 0 ? " SP" + version.ServicePackMajor : string.Empty);

            EvaluateOs(report, version, editionId, productName);

            var release = ReadRegistry(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full",
                "Release",
                string.Empty);
            report.DotNetRelease = release;
            int releaseNumber;
            report.DotNet48 = int.TryParse(release, out releaseNumber) && releaseNumber >= 528040;

            try
            {
                var webViewVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
                report.WebView2Version = webViewVersion ?? string.Empty;
                report.WebView2Available = !string.IsNullOrWhiteSpace(webViewVersion);

                Version parsed;
                if (report.WebView2Available && Version.TryParse(webViewVersion, out parsed))
                {
                    report.WebView2LegacyCompatible = version.Major >= 10 || parsed.Major == 109;
                }
                else
                {
                    report.WebView2LegacyCompatible = false;
                }
            }
            catch (Exception exception)
            {
                report.WebView2Available = false;
                report.WebView2LegacyCompatible = false;
                report.WebView2Error = exception.GetType().Name + ": " + exception.Message;
            }

            report.D3DFeatureLevel = NativeMethods.GetD3DFeatureLevel();
            report.SoftwareRenderingRecommended = report.D3DFeatureLevel == 0 ||
                (report.D3DFeatureLevel > 0 && report.D3DFeatureLevel < 0xA000);
            report.D3DDescription = FeatureLevelName(report.D3DFeatureLevel);

            return report;
        }

        public string ToText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("ARA Assessment " + AppVersion);
            builder.AppendLine("Collected: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine("OS: " + OsDescription);
            builder.AppendLine("OS supported: " + OsSupported);
            if (!string.IsNullOrWhiteSpace(OsError))
            {
                builder.AppendLine("OS error: " + OsError);
            }
            builder.AppendLine(".NET 4.8: " + DotNet48 + " (release " + DotNetRelease + ")");
            builder.AppendLine("WebView2: " + (WebView2Available ? WebView2Version : "not available"));
            builder.AppendLine("WebView2 legacy compatible: " + WebView2LegacyCompatible);
            if (!string.IsNullOrWhiteSpace(WebView2Error))
            {
                builder.AppendLine("WebView2 error: " + WebView2Error);
            }
            builder.AppendLine("D3D11 feature level: " + D3DDescription);
            builder.AppendLine("Software rendering recommended: " + SoftwareRenderingRecommended);
            builder.AppendLine("Logs: " + LogDirectory);
            return builder.ToString();
        }

        public string WriteReport()
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(
                LogDirectory,
                "diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".txt");
            File.WriteAllText(path, ToText(), Encoding.UTF8);
            return path;
        }

        public string GetWebViewError()
        {
            if (!WebView2Available)
            {
                return "Microsoft Edge WebView2 Runtime tidak ditemukan. " +
                       "Jalankan installer ulang atau hubungi pengawas.";
            }

            if (!WebView2LegacyCompatible)
            {
                return "WebView2 Runtime 109.0.1518.140 diperlukan pada Windows 7/8.1 dan Windows 10 1511/1607/1703. " +
                       "Versi terdeteksi: " + WebView2Version + ".";
            }

            return string.Empty;
        }

        private static void EvaluateOs(
            CapabilityReport report,
            WindowsVersion version,
            string editionId,
            string productName)
        {
            var major = version.Major;
            var minor = version.Minor;
            var build = version.Build;
            var ltsc = IsLtsc(editionId, productName);

            if (major == 6 && minor == 1)
            {
                report.OsSupported = version.ServicePackMajor >= 1;
                if (!report.OsSupported)
                {
                    report.OsError = "Windows 7 memerlukan Service Pack 1.";
                }
                return;
            }

            if (major == 6 && (minor == 2 || minor == 3))
            {
                report.OsSupported = true;
                return;
            }

            if (major == 10 && build >= 22000)
            {
                report.OsSupported = true;
                return;
            }

            if (major == 10)
            {
                if (build >= 16299 || ltsc)
                {
                    report.OsSupported = true;
                    return;
                }

                if (build >= 10586)
                {
                    report.OsSupported = true;
                    return;
                }

                report.OsSupported = false;
                report.OsError = "Windows 10 versi ini tidak didukung. " +
                                 "Diperlukan Windows 10 versi 1511 (build 10586) atau lebih baru.";
                return;
            }

            report.OsSupported = false;
            report.OsError = "Windows ini tidak didukung. ARA Assessment memerlukan Windows 7 SP1 atau lebih baru.";
        }

        private static bool IsLtsc(string editionId, string productName)
        {
            var edition = (editionId ?? string.Empty).ToLowerInvariant();
            var product = (productName ?? string.Empty).ToLowerInvariant();
            return edition.Contains("enterprises") ||
                   edition.Contains("enterprisesn") ||
                   product.Contains("ltsc") ||
                   product.Contains("ltsb");
        }

        private static string FeatureLevelName(int level)
        {
            switch (level)
            {
                case 0x9100: return "9_1 (0x9100)";
                case 0x9200: return "9_2 (0x9200)";
                case 0x9300: return "9_3 (0x9300)";
                case 0xA000: return "10_0 (0xA000)";
                case 0xA100: return "10_1 (0xA100)";
                case 0xB000: return "11_0 (0xB000)";
                case 0xB100: return "11_1 (0xB100)";
                case 0xC000: return "12_0 (0xC000)";
                case 0xC100: return "12_1 (0xC100)";
                default: return level == 0 ? "unavailable" : "unknown (0x" + level.ToString("X4") + ")";
            }
        }

        private static string ReadRegistry(string key, string name, string fallback)
        {
            try
            {
                var value = Registry.GetValue(key, name, null);
                return value == null ? fallback : value.ToString();
            }
            catch
            {
                return fallback;
            }
        }
    }
}

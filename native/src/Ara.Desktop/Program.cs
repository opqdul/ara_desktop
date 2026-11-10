using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Ara.Core;
using Ara.Desktop.UI;

namespace Ara.Desktop
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            var options = Options.Parse(args);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Log.Initialize();
            Log.Info("=== " + AppConfig.AppName + " " + AppConfig.AppVersion + " ===");
            Log.Info("Arguments: " + string.Join(" ", args));

            var report = CapabilityReport.Collect();
            Log.Info(report.ToText());

            if (options.Diagnostics)
            {
                var path = report.WriteReport();
                Log.Info("Diagnostics report: " + path);
                if (!options.Quiet)
                {
                    MessageBox.Show(
                        report.ToText() + Environment.NewLine + "Report: " + path,
                        AppConfig.AppName + " Diagnostics",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                return 0;
            }

            var disableGpu = options.DisableGpu || report.SoftwareRenderingRecommended;

            if (!report.OsSupported ||
                !report.WebView2Available ||
                !report.WebView2LegacyCompatible)
            {
                var error = !report.OsSupported ? report.OsError : report.GetWebViewError();
                if (options.SmokeTest)
                {
                    WriteSmokeFailure(report, disableGpu, error);
                    return 1;
                }

                using (var form = new ErrorForm(
                    Strings.ErrorCapability,
                    error,
                    report.ToText(),
                    false))
                {
                    Application.Run(form);
                }
                return 1;
            }

            if (options.SmokeTest)
            {
                using (var form = new MainForm(report, disableGpu, true))
                {
                    Application.Run(form);
                    return form.ExitCode;
                }
            }

            bool created;
            using (var mutex = new Mutex(true, @"Local\ARAAssessmentDesktop", out created))
            {
                if (!created)
                {
                    FocusExisting();
                    return 0;
                }

                using (var form = new MainForm(report, disableGpu, false))
                {
                    Application.Run(form);
                }
            }

            return 0;
        }

        private static void FocusExisting()
        {
            var hwnd = NativeMethods.FindWindow(null, AppConfig.AppName);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.ShowWindow(hwnd, 5);
            NativeMethods.SetForegroundWindow(hwnd);
        }

        private static void WriteSmokeFailure(CapabilityReport report, bool disableGpu, string error)
        {
            try
            {
                Directory.CreateDirectory(AppConfig.LogDirectory);
                var result = new Dictionary<string, object>
                {
                    { "login_rendered", false },
                    { "navigation_success", false },
                    { "disable_gpu", disableGpu },
                    { "error", error ?? string.Empty },
                    { "webview2_version", report.WebView2Version ?? string.Empty },
                    { "os_description", report.OsDescription ?? string.Empty },
                    { "os_major", report.OsMajor },
                    { "os_minor", report.OsMinor },
                    { "os_build", report.OsBuild },
                    { "architecture", Environment.Is64BitProcess ? "x64" : "x86" },
                    { "d3d_feature_level", report.D3DDescription ?? string.Empty },
                    { "software_rendering", report.SoftwareRenderingRecommended },
                    { "final_url", string.Empty },
                    { "title", string.Empty },
                    { "body_text_length", 0 },
                    { "collected_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                File.WriteAllText(
                    Path.Combine(AppConfig.LogDirectory, "smoke-result.json"),
                    new JavaScriptSerializer().Serialize(result));
            }
            catch
            {
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Error("UI thread exception", e.Exception);
            try
            {
                MessageBox.Show(
                    Strings.ErrorUnexpected + Environment.NewLine + e.Exception.Message,
                    AppConfig.AppName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Error("Unhandled exception", exception);
        }
    }

    internal sealed class Options
    {
        public bool Diagnostics { get; private set; }
        public bool SmokeTest { get; private set; }
        public bool Quiet { get; private set; }
        public bool DisableGpu { get; private set; }

        public static Options Parse(string[] args)
        {
            var options = new Options();
            foreach (var arg in args ?? new string[0])
            {
                switch ((arg ?? string.Empty).ToLowerInvariant())
                {
                    case "--diagnostics":
                        options.Diagnostics = true;
                        break;
                    case "--smoke-test":
                        options.SmokeTest = true;
                        break;
                    case "--quiet":
                        options.Quiet = true;
                        break;
                    case "--disable-gpu":
                        options.DisableGpu = true;
                        break;
                }
            }
            return options;
        }
    }
}

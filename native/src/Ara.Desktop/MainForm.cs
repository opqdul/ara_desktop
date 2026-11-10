using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Ara.Core;
using Ara.Desktop.Kiosk;
using Ara.Desktop.UI;

namespace Ara.Desktop
{
    public sealed class MainForm : Form
    {
        private readonly CapabilityReport _capability;
        private readonly bool _disableGpu;
        private readonly bool _smokeTest;
        private readonly TopBar _topBar;
        private readonly Panel _alertBar;
        private readonly Label _alertLabel;
        private readonly Timer _alertTimer;
        private readonly Panel _webContainer;
        private readonly WindowGuard _guard;
        private readonly SupervisorAuth _auth = new SupervisorAuth();
        private readonly ViolationReporter _reporter = new ViolationReporter();
        private readonly AssessmentDetector _detector = new AssessmentDetector();

        private WebViewHost _host;
        private ForegroundWatcher _watcher;
        private LoadingForm _loading;
        private Timer _smokeTimer;
        private ExamContext _context = new ExamContext();
        private AssessmentMode _mode = AssessmentMode.Normal;
        private bool _manualOverride;
        private bool _contentReady;
        private bool _smokeCompleted;
        private int _violationCount;

        public int ExitCode { get; private set; }

        public MainForm(CapabilityReport capability, bool disableGpu, bool smokeTest)
        {
            _capability = capability;
            _disableGpu = disableGpu;
            _smokeTest = smokeTest;

            Text = Strings.AppName;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            }
            catch
            {
                Icon = SystemIcons.Application;
            }

            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1280, 720);
            MinimumSize = new Size(1024, 600);
            BackColor = Color.FromArgb(43, 43, 43);
            KeyPreview = true;
            DoubleBuffered = true;

            _guard = new WindowGuard(this, OnCloseAttempt);

            _topBar = new TopBar();
            _topBar.RefreshClicked += (sender, args) => _host?.Reload();
            _topBar.SupervisorRequested += (sender, args) => ShowSupervisor();
            _topBar.OfficeRequested += OpenOffice;

            _alertBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(125, 30, 30),
                Visible = false
            };

            _alertLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(14, 0, 14, 0)
            };
            _alertBar.Controls.Add(_alertLabel);

            _alertTimer = new Timer { Interval = 4000 };
            _alertTimer.Tick += (sender, args) =>
            {
                _alertTimer.Stop();
                _alertBar.Visible = false;
            };

            _webContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Controls.Add(_webContainer);
            Controls.Add(_alertBar);
            Controls.Add(_topBar);

            Shown += (sender, args) =>
            {
                if (_smokeTest)
                {
                    _smokeTimer = new Timer { Interval = 60000 };
                    _smokeTimer.Tick += async (s, e) =>
                        await CompleteSmokeTestAsync(false, "timeout waiting for navigation");
                    _smokeTimer.Start();
                }

                ShowLoading();
                _ = InitializeHostAsync();
            };
            FormClosing += OnFormClosing;
        }

        private async Task InitializeHostAsync()
        {
            try
            {
                var userData = _smokeTest
                    ? Path.Combine(AppConfig.LocalAppData, "WebView2Smoke")
                    : AppConfig.WebViewDataFolder;

                _host = new WebViewHost(this, _webContainer, userData, _disableGpu, () => _mode == AssessmentMode.Exam);
                _host.Status += UpdateLoadingStatus;
                _host.NavigationStarted += EvaluateModeFromUrl;
                _host.NavigationSucceeded += url =>
                {
                    EvaluateModeFromUrl(url);
                    OnContentReady();
                };
                _host.NavigationFailed += OnNavigationFailed;
                _host.UrlChanged += EvaluateModeFromUrl;
                _host.DomReady += url =>
                {
                    EvaluateModeFromUrl(url);
                    OnContentReady();
                };
                _host.ModeChanged += mode =>
                {
                    if (!_manualOverride)
                    {
                        SetMode(mode);
                    }
                };
                _host.ContextChanged += context => _context = context;
                _host.ViolationDetected += (type, label, description) =>
                {
                    _ = ReportViolationNativeAsync(type, label, description);
                };
                _host.SupervisorRequested += ShowSupervisor;
                _host.OfficeUrlRequested += url => _ = HandleOfficeUrlAsync(url);
                _host.ExternalUrlRequested += HandleExternalUrl;
                _host.DownloadBlocked += name =>
                {
                    ShowAlert(Strings.DownloadBlocked);
                    _ = ReportViolationAsync(
                        "unauthorized_download",
                        "Unduhan Diblokir",
                        Strings.DownloadBlocked + " (" + name + ")");
                };
                _host.DownloadFinished += OnDownloadFinished;
                _host.InitializationFailed += OnInitializationFailed;

                await _host.InitializeAsync();
            }
            catch (Exception exception)
            {
                OnInitializationFailed(exception);
            }
        }

        private void UpdateLoadingStatus(string message)
        {
            if (_loading != null)
            {
                _loading.SetStatus(message);
            }
        }

        private void ShowLoading()
        {
            if (_loading != null)
            {
                return;
            }

            _contentReady = false;
            _loading = new LoadingForm(Icon);
            Enabled = false;
            _loading.Show(this);
        }

        private void CloseLoading()
        {
            if (_loading != null)
            {
                _loading.Close();
                _loading.Dispose();
                _loading = null;
            }
            Enabled = true;
        }

        private void OnContentReady()
        {
            if (_contentReady)
            {
                return;
            }

            _contentReady = true;
            CloseLoading();
            _webContainer.Visible = true;

            if (_smokeTest)
            {
                _ = CompleteSmokeTestAsync(true, string.Empty);
                return;
            }

            EvaluateModeFromUrl(_host != null ? _host.CurrentUrl : string.Empty);
            Log.Info("Portal loaded.");
        }

        private void OnNavigationFailed(string status)
        {
            CloseLoading();

            if (_smokeTest)
            {
                _ = CompleteSmokeTestAsync(false, status);
                return;
            }

            ShowError(
                Strings.ErrorTitle,
                Strings.ErrorNavigation + Environment.NewLine + status,
                true,
                false);
        }

        private void OnInitializationFailed(Exception exception)
        {
            Log.Error("WebView initialization failed", exception);
            CloseLoading();

            if (_smokeTest)
            {
                _ = CompleteSmokeTestAsync(false, "initialization: " + exception.Message);
                return;
            }

            ShowError(
                Strings.ErrorInitTitle,
                Strings.ErrorInit + Environment.NewLine + exception.Message,
                true,
                true);
        }

        private void ShowError(string title, string message, bool canRetry, bool restartOnRetry)
        {
            CloseLoading();
            var diagnostics = BuildDiagnostics();

            using (var form = new ErrorForm(title, message, diagnostics, canRetry))
            {
                var result = form.ShowDialog(this);
                if (result == DialogResult.Retry && canRetry)
                {
                    if (restartOnRetry)
                    {
                        _guard.AllowExit = true;
                        Application.Restart();
                        return;
                    }

                    ShowLoading();
                    _host?.Navigate(AppConfig.ProductionUrl);
                    return;
                }
            }

            _guard.AllowExit = true;
            Close();
        }

        private void EvaluateModeFromUrl(string url)
        {
            if (_manualOverride || string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            SetMode(_detector.EvaluateUrl(url));
        }

        private void SetMode(AssessmentMode mode)
        {
            if (_mode == mode)
            {
                return;
            }

            _mode = mode;
            _topBar.SetExam(mode == AssessmentMode.Exam);
            _guard.SetExam(mode == AssessmentMode.Exam);

            if (mode == AssessmentMode.Exam)
            {
                StartWatcher();
            }
            else
            {
                StopWatcher();
            }

            Log.Info("Mode: " + mode);
        }

        private void StartWatcher()
        {
            if (_watcher != null)
            {
                return;
            }

            _watcher = new ForegroundWatcher(Handle, this, OnForegroundPolicy);
            _watcher.Start();
        }

        private void StopWatcher()
        {
            if (_watcher == null)
            {
                return;
            }

            _watcher.Dispose();
            _watcher = null;
        }

        private void OnForegroundPolicy(IntPtr hwnd, string exe, KioskPolicyEvaluation evaluation)
        {
            if (evaluation.IsEmergency)
            {
                ShowAlert(Strings.EmergencyTaskManager);
                _ = ReportViolationAsync(
                    "unauthorized_app",
                    "Task Manager (darurat)",
                    Strings.EmergencyTaskManager);
                return;
            }

            ShowAlert(string.Format(Strings.UnauthorizedApp, exe));
            _ = ReportViolationAsync(
                "unauthorized_app",
                "Membuka Aplikasi Terlarang (" + exe + ")",
                evaluation.Reason);
        }

        private void OnCloseAttempt()
        {
            ShowAlert(Strings.CloseBlocked);
            _ = ReportViolationAsync(
                "app_exit",
                "Percobaan Menutup Aplikasi Saat Ujian",
                Strings.CloseBlocked);
        }

        private async Task ReportViolationAsync(string type, string label, string description)
        {
            _violationCount++;
            var fullDescription = description + " (Pelanggaran ke-" + _violationCount + ")";
            ShowAlert(fullDescription);
            Log.Info("Violation: " + type);

            if (_host == null)
            {
                await ReportViolationNativeAsync(type, label, fullDescription);
                return;
            }

            try
            {
                var script = "if (typeof window.recordAraDesktopViolation === 'function') { " +
                             "window.recordAraDesktopViolation(" +
                             Json(type) + "," + Json(label) + "," + Json(fullDescription) +
                             ",'ara_desktop_windows'); }";
                var result = await _host.ExecuteScriptAsync(script);
                if (!string.Equals(result, "true", StringComparison.OrdinalIgnoreCase))
                {
                    await _reporter.ReportAsync(
                        _context,
                        type,
                        label,
                        fullDescription,
                        _host.GetCookieHeaderAsync);
                }
            }
            catch (Exception exception)
            {
                Log.Warn("Violation reporting failed: " + exception.GetType().Name);
                await ReportViolationNativeAsync(type, label, fullDescription);
            }
        }

        private async Task ReportViolationNativeAsync(string type, string label, string description)
        {
            try
            {
                await _reporter.ReportAsync(
                    _context,
                    type,
                    label,
                    description,
                    _host != null ? (Func<string, Task<string>>)_host.GetCookieHeaderAsync : null);
            }
            catch (Exception exception)
            {
                Log.Warn("Native violation report failed: " + exception.GetType().Name);
            }
        }

        private async Task HandleOfficeUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return;
            }

            var scheme = uri.Scheme.ToLowerInvariant();
            if (scheme == "ms-word" || scheme == "ms-excel" || scheme == "ms-powerpoint" || scheme == "ms-office")
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    ShowAlert(Strings.OfficeMissing);
                }
                return;
            }

            if (scheme != "http" && scheme != "https")
            {
                HandleExternalUrl(url);
                return;
            }

            if (!OfficeDocumentService.IsOfficeDocumentUrl(uri))
            {
                HandleExternalUrl(url);
                return;
            }

            ShowAlert(Strings.OfficeOpening);
            if (_host == null)
            {
                ShowAlert(Strings.OfficeMissing);
                return;
            }

            var cookies = await _host.GetCookieHeaderAsync(url);
            var result = await OfficeDocumentService.DownloadAndOpenAsync(
                uri,
                null,
                null,
                cookies,
                null);
            ShowAlert(result.Message);
        }

        private void OnDownloadFinished(string path, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                var kind = OfficeDocumentService.DetectKind(Path.GetFileName(path), null, mimeType);
                if (kind.HasValue)
                {
                    var result = OfficeLauncher.OpenDocument(path, kind.Value);
                    ShowAlert(result.Message);
                    return;
                }

                if (_mode == AssessmentMode.Normal)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                    return;
                }

                File.Delete(path);
                ShowAlert(Strings.DownloadBlocked);
            }
            catch (Exception exception)
            {
                Log.Warn("Download open failed: " + exception.GetType().Name);
                ShowAlert(Strings.DownloadFailed);
            }
        }

        private void HandleExternalUrl(string url)
        {
            if (_mode == AssessmentMode.Exam)
            {
                ShowAlert(Strings.ExternalBlocked);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                ShowAlert(Strings.NotAvailable);
            }
        }

        private void OpenOffice(OfficeDocumentKind kind)
        {
            var result = OfficeLauncher.OpenApp(kind);
            ShowAlert(result.Message);
            Log.Info("Office open: " + kind + " success=" + result.Success);
        }

        private void ShowSupervisor()
        {
            using (var form = new SupervisorForm(_auth, BuildDiagnostics()))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var action = form.SelectedAction;
                Log.Info("Supervisor action: " + action);

                switch (action)
                {
                    case SupervisorAction.Normal:
                        _manualOverride = true;
                        SetMode(AssessmentMode.Normal);
                        break;

                    case SupervisorAction.Exam:
                        _manualOverride = true;
                        SetMode(AssessmentMode.Exam);
                        break;

                    case SupervisorAction.Auto:
                        _manualOverride = false;
                        EvaluateModeFromUrl(_host != null ? _host.CurrentUrl : string.Empty);
                        break;

                    case SupervisorAction.Word:
                        OpenOffice(OfficeDocumentKind.Word);
                        break;

                    case SupervisorAction.Excel:
                        OpenOffice(OfficeDocumentKind.Excel);
                        break;

                    case SupervisorAction.PowerPoint:
                        OpenOffice(OfficeDocumentKind.PowerPoint);
                        break;

                    case SupervisorAction.Reload:
                        _host?.Reload();
                        break;

                    case SupervisorAction.OpenLogs:
                        try
                        {
                            Process.Start("explorer.exe", AppConfig.LogDirectory);
                        }
                        catch
                        {
                            ShowAlert(Strings.LogFolderMissing);
                        }
                        break;

                    case SupervisorAction.CopyDiagnostics:
                        try
                        {
                            Clipboard.SetText(BuildDiagnostics());
                            ShowAlert(Strings.DiagnosticsCopied);
                        }
                        catch
                        {
                        }
                        break;

                    case SupervisorAction.Exit:
                        _guard.AllowExit = true;
                        Close();
                        break;
                }
            }
        }

        private void ShowAlert(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _alertLabel.Text = message;
            _alertBar.Visible = true;
            _alertBar.BringToFront();
            _alertTimer.Stop();
            _alertTimer.Start();
        }

        private string BuildDiagnostics()
        {
            return _capability.ToText() +
                   Environment.NewLine +
                   "Current URL: " + (_host != null ? _host.CurrentUrl : "(not ready)") +
                   Environment.NewLine +
                   "Mode: " + _mode +
                   Environment.NewLine +
                   "Violations: " + _violationCount +
                   Environment.NewLine +
                   "Log: " + AppConfig.LogDirectory;
        }

        private static string Json(string value)
        {
            return new JavaScriptSerializer().Serialize(value ?? string.Empty);
        }

        private async Task CompleteSmokeTestAsync(bool navigationSuccess, string error)
        {
            if (_smokeCompleted)
            {
                return;
            }

            _smokeCompleted = true;
            if (_smokeTimer != null)
            {
                _smokeTimer.Stop();
            }

            var title = string.Empty;
            var bodyLength = 0;
            var finalUrl = string.Empty;

            try
            {
                await Task.Delay(1000);
                if (_host != null)
                {
                    finalUrl = _host.CurrentUrl;
                    var titleJson = await _host.ExecuteScriptAsync("document.title");
                    var bodyJson = await _host.ExecuteScriptAsync("document.body ? document.body.innerText.length : 0");
                    title = Unquote(titleJson);
                    int.TryParse(bodyJson, out bodyLength);
                }
            }
            catch
            {
            }

            var loginRendered = navigationSuccess && bodyLength > 0;
            var result = new Dictionary<string, object>
            {
                { "login_rendered", loginRendered },
                { "navigation_success", navigationSuccess },
                { "disable_gpu", _disableGpu },
                { "error", error ?? string.Empty },
                { "webview2_version", _capability.WebView2Version ?? string.Empty },
                { "os_description", _capability.OsDescription ?? string.Empty },
                { "os_major", _capability.OsMajor },
                { "os_minor", _capability.OsMinor },
                { "os_build", _capability.OsBuild },
                { "architecture", Environment.Is64BitProcess ? "x64" : "x86" },
                { "d3d_feature_level", _capability.D3DDescription ?? string.Empty },
                { "software_rendering", _capability.SoftwareRenderingRecommended },
                { "final_url", finalUrl },
                { "title", title },
                { "body_text_length", bodyLength },
                { "collected_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };

            try
            {
                Directory.CreateDirectory(AppConfig.LogDirectory);
                File.WriteAllText(
                    Path.Combine(AppConfig.LogDirectory, "smoke-result.json"),
                    new JavaScriptSerializer().Serialize(result));
            }
            catch
            {
            }

            ExitCode = loginRendered ? 0 : 1;
            _guard.AllowExit = true;
            Close();
        }

        private static string Unquote(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            try
            {
                return new JavaScriptSerializer().Deserialize<string>(json) ?? string.Empty;
            }
            catch
            {
                return json.Trim('"');
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.F12 &&
                ((keyData & Keys.Control) == Keys.Control ||
                 (keyData & Keys.Shift) == Keys.Shift ||
                 (keyData & Keys.Alt) == Keys.Alt))
            {
                ShowSupervisor();
                return true;
            }

            if (keyData == Keys.F5 || keyData == (Keys.Control | Keys.R))
            {
                _host?.Reload();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            if (_guard != null && _guard.HandleMessage(ref m))
            {
                return;
            }

            base.WndProc(ref m);
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_guard.HandleFormClosing(e))
            {
                return;
            }

            _alertTimer.Stop();
            if (_smokeTimer != null)
            {
                _smokeTimer.Stop();
            }
            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
            _reporter.Dispose();
        }
    }
}

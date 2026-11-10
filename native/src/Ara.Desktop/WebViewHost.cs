using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Ara.Core;
using Ara.Desktop.UI;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Ara.Desktop
{
    public sealed class WebViewHost : IDisposable
    {
        private readonly Form _owner;
        private readonly WebView2 _webView;
        private readonly string _userDataFolder;
        private readonly bool _disableGpu;
        private readonly Func<bool> _isExam;
        private readonly AssessmentDetector _detector = new AssessmentDetector();
        private CoreWebView2 _core;
        private int _retryCount;
        private string _lastUrl = string.Empty;
        private bool _disposed;

        public event Action<string> Status;
        public event Action<string> NavigationStarted;
        public event Action<string> NavigationSucceeded;
        public event Action<string> NavigationFailed;
        public event Action<string> UrlChanged;
        public event Action<string> DomReady;
        public event Action<AssessmentMode> ModeChanged;
        public event Action<ExamContext> ContextChanged;
        public event Action<string, string, string> ViolationDetected;
        public event Action SupervisorRequested;
        public event Action<string> OfficeUrlRequested;
        public event Action<string> ExternalUrlRequested;
        public event Action<string> DownloadBlocked;
        public event Action<string, string> DownloadFinished;
        public event Action<Exception> InitializationFailed;

        public WebViewHost(
            Form owner,
            Panel container,
            string userDataFolder,
            bool disableGpu,
            Func<bool> isExam)
        {
            _owner = owner;
            _userDataFolder = userDataFolder;
            _disableGpu = disableGpu;
            _isExam = isExam;

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.White
            };
            container.Controls.Add(_webView);
        }

        public string CurrentUrl
        {
            get { return _core != null ? _core.Source : _lastUrl; }
        }

        public bool IsReady
        {
            get { return _core != null; }
        }

        public async Task InitializeAsync()
        {
            try
            {
                Directory.CreateDirectory(_userDataFolder);

                var options = new CoreWebView2EnvironmentOptions();
                if (_disableGpu)
                {
                    options.AdditionalBrowserArguments = "--disable-gpu";
                }

                Status?.Invoke("Menyiapkan tampilan web...");
                var environment = await CoreWebView2Environment.CreateAsync(
                    null,
                    _userDataFolder,
                    options).ConfigureAwait(true);
                await _webView.EnsureCoreWebView2Async(environment).ConfigureAwait(true);

                _core = _webView.CoreWebView2;
                if (_core == null)
                {
                    throw new InvalidOperationException("CoreWebView2 tidak tersedia.");
                }

                var settings = _core.Settings;
                settings.AreDevToolsEnabled = false;
                settings.AreDefaultContextMenusEnabled = false;
                settings.AreDefaultScriptDialogsEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
                settings.IsStatusBarEnabled = false;
                settings.IsZoomControlEnabled = false;
                settings.AreHostObjectsAllowed = false;
                settings.IsWebMessageEnabled = true;

                _core.NavigationStarting += OnNavigationStarting;
                _core.NavigationCompleted += OnNavigationCompleted;
                _core.SourceChanged += OnSourceChanged;
                _core.HistoryChanged += OnHistoryChanged;
                _core.DOMContentLoaded += OnDomContentLoaded;
                _core.WebMessageReceived += OnWebMessageReceived;
                _core.PermissionRequested += OnPermissionRequested;
                _core.NewWindowRequested += OnNewWindowRequested;
                _core.DownloadStarting += OnDownloadStarting;
                _core.ScriptDialogOpening += OnScriptDialogOpening;
                _core.ProcessFailed += OnProcessFailed;

                await _core.AddScriptToExecuteOnDocumentCreatedAsync(
                    AssessmentDetector.InjectedScript).ConfigureAwait(true);

                Status?.Invoke("Memuat portal asesmen...");
                _lastUrl = AppConfig.ProductionUrl;
                _core.Navigate(AppConfig.ProductionUrl);
            }
            catch (Exception exception)
            {
                Log.Error("WebView2 initialization failed", exception);
                InitializationFailed?.Invoke(exception);
            }
        }

        public void Reload()
        {
            if (_core == null)
            {
                return;
            }

            try
            {
                _core.Reload();
            }
            catch (Exception exception)
            {
                Log.Warn("Reload failed: " + exception.GetType().Name);
            }
        }

        public void Navigate(string url)
        {
            if (_core == null || string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                _lastUrl = url;
                _core.Navigate(url);
            }
            catch (Exception exception)
            {
                Log.Warn("Navigate failed: " + exception.GetType().Name);
            }
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            if (_core == null)
            {
                return "false";
            }

            try
            {
                return await _core.ExecuteScriptAsync(script).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                Log.Warn("ExecuteScript failed: " + exception.GetType().Name);
                return "false";
            }
        }

        public async Task<string> GetCookieHeaderAsync(string url)
        {
            if (_core == null || string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            try
            {
                var cookies = await _core.CookieManager.GetCookiesAsync(url).ConfigureAwait(true);
                return string.Join("; ", cookies.Select(cookie => cookie.Name + "=" + cookie.Value));
            }
            catch (Exception exception)
            {
                Log.Warn("Cookie read failed: " + exception.GetType().Name);
                return string.Empty;
            }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var url = e.Uri ?? string.Empty;
            _lastUrl = url;
            NavigationStarted?.Invoke(url);

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                e.Cancel = true;
                return;
            }

            var scheme = uri.Scheme.ToLowerInvariant();

            if (scheme == "ms-word" || scheme == "ms-excel" || scheme == "ms-powerpoint" || scheme == "ms-office")
            {
                e.Cancel = true;
                OfficeUrlRequested?.Invoke(url);
                return;
            }

            if ((scheme == "http" || scheme == "https") && OfficeDocumentService.IsOfficeDocumentUrl(uri))
            {
                e.Cancel = true;
                OfficeUrlRequested?.Invoke(url);
                return;
            }

            if (scheme == "http" ||
                scheme == "https" ||
                scheme == "about" ||
                scheme == "data" ||
                scheme == "blob" ||
                scheme == "javascript" ||
                scheme == "file")
            {
                return;
            }

            e.Cancel = true;
            ExternalUrlRequested?.Invoke(url);
        }

        private async void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            var url = _core != null ? _core.Source : _lastUrl;
            if (e.IsSuccess)
            {
                _retryCount = 0;
                NavigationSucceeded?.Invoke(url ?? string.Empty);
                return;
            }

            var status = e.WebErrorStatus.ToString();
            if (status.IndexOf("Canceled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                status.IndexOf("Cancelled", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            if (_retryCount < 2)
            {
                _retryCount++;
                Status?.Invoke("Memuat ulang halaman...");
                try
                {
                    await Task.Delay(800).ConfigureAwait(true);
                    if (!_disposed && _core != null)
                    {
                        _core.Navigate(string.IsNullOrWhiteSpace(_lastUrl)
                            ? AppConfig.ProductionUrl
                            : _lastUrl);
                    }
                }
                catch (Exception exception)
                {
                    Log.Warn("Navigation retry failed: " + exception.GetType().Name);
                }
                return;
            }

            NavigationFailed?.Invoke(status);
        }

        private void OnSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (_core != null)
            {
                UrlChanged?.Invoke(_core.Source);
            }
        }

        private void OnHistoryChanged(object sender, object e)
        {
            if (_core != null)
            {
                UrlChanged?.Invoke(_core.Source);
            }
        }

        private void OnDomContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (_core != null)
            {
                DomReady?.Invoke(_core.Source);
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var serializer = new JavaScriptSerializer();
                if (json.TrimStart().StartsWith("\"", StringComparison.Ordinal))
                {
                    json = serializer.Deserialize<string>(json);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var message = serializer.Deserialize<Dictionary<string, object>>(json);
                if (message == null)
                {
                    return;
                }

                object handlerValue;
                if (!message.TryGetValue("handler", out handlerValue))
                {
                    return;
                }

                var handler = Convert.ToString(handlerValue);
                var args = new List<object>();
                object argsValue;
                if (message.TryGetValue("args", out argsValue) && argsValue is IEnumerable)
                {
                    foreach (var item in (IEnumerable)argsValue)
                    {
                        args.Add(item);
                    }
                }

                switch (handler)
                {
                    case "onAssessmentStateChange":
                        if (args.Count > 0)
                        {
                            ModeChanged?.Invoke(_detector.EvaluateMode(Convert.ToString(args[0])));
                        }
                        break;

                    case "onExamContextUpdate":
                        if (args.Count > 0)
                        {
                            var dictionary = args[0] as Dictionary<string, object>;
                            if (dictionary != null)
                            {
                                var context = new ExamContext();
                                context.UpdateFrom(dictionary);
                                ContextChanged?.Invoke(context);
                            }
                        }
                        break;

                    case "onViolation":
                        if (args.Count >= 3)
                        {
                            ViolationDetected?.Invoke(
                                Convert.ToString(args[0]),
                                Convert.ToString(args[1]),
                                Convert.ToString(args[2]));
                        }
                        break;

                    case "onSupervisorPanelTrigger":
                        SupervisorRequested?.Invoke();
                        break;

                    case "onDomReady":
                        DomReady?.Invoke(args.Count > 0
                            ? Convert.ToString(args[0])
                            : (_core != null ? _core.Source : _lastUrl));
                        break;

                    case "onExternalOrOfficeUrl":
                        if (args.Count > 0)
                        {
                            OfficeUrlRequested?.Invoke(Convert.ToString(args[0]));
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.Warn("Web message parse failed: " + exception.GetType().Name);
            }
        }

        private void OnPermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            var allowedOrigin = IsAssessmentOrigin(e.Uri);
            var allowedKind = e.PermissionKind == CoreWebView2PermissionKind.Camera ||
                              e.PermissionKind == CoreWebView2PermissionKind.Microphone;

            e.State = allowedOrigin && allowedKind
                ? CoreWebView2PermissionState.Allow
                : CoreWebView2PermissionState.Deny;
            e.Handled = true;

            Log.Info("Permission " + e.PermissionKind + " for " + e.Uri + " -> " + e.State);
        }

        private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            var url = e.Uri ?? string.Empty;
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return;
            }

            var scheme = uri.Scheme.ToLowerInvariant();
            if (scheme == "ms-word" || scheme == "ms-excel" || scheme == "ms-powerpoint" || scheme == "ms-office" ||
                ((scheme == "http" || scheme == "https") && OfficeDocumentService.IsOfficeDocumentUrl(uri)))
            {
                OfficeUrlRequested?.Invoke(url);
                return;
            }

            if (scheme == "http" || scheme == "https")
            {
                _core.Navigate(url);
                return;
            }

            ExternalUrlRequested?.Invoke(url);
        }

        private void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            var download = e.DownloadOperation;
            if (download == null)
            {
                return;
            }

            Uri uri;
            Uri.TryCreate(download.Uri, UriKind.Absolute, out uri);
            var suggested = Path.GetFileName(e.ResultFilePath);
            var kind = OfficeDocumentService.DetectKind(suggested, uri, download.MimeType);

            if (_isExam != null && _isExam() && !kind.HasValue)
            {
                e.Cancel = true;
                e.Handled = true;
                DownloadBlocked?.Invoke(string.IsNullOrWhiteSpace(suggested) ? "file" : suggested);
                return;
            }

            var fileName = OfficeDocumentService.ResolveFileName(
                suggested,
                download.ContentDisposition,
                uri,
                download.MimeType);
            var folder = Path.Combine(
                AppConfig.DownloadsRoot,
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, fileName);

            e.ResultFilePath = path;
            e.Handled = true;

            download.StateChanged += (sender2, args2) =>
            {
                if (download.State == CoreWebView2DownloadState.Completed)
                {
                    DownloadFinished?.Invoke(path, download.MimeType ?? string.Empty);
                }
            };
        }

        private void OnScriptDialogOpening(object sender, CoreWebView2ScriptDialogOpeningEventArgs e)
        {
            switch (e.Kind)
            {
                case CoreWebView2ScriptDialogKind.Alert:
                    MessageBox.Show(
                        _owner,
                        e.Message,
                        "Informasi Portal",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    e.Accept();
                    break;

                case CoreWebView2ScriptDialogKind.Confirm:
                    if (MessageBox.Show(
                        _owner,
                        e.Message,
                        "Konfirmasi",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        e.Accept();
                    }
                    break;

                case CoreWebView2ScriptDialogKind.Prompt:
                    using (var form = new PromptForm(e.Message, e.DefaultText))
                    {
                        if (form.ShowDialog(_owner) == DialogResult.OK)
                        {
                            e.ResultText = form.Value;
                            e.Accept();
                        }
                    }
                    break;
            }
        }

        private void OnProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            Log.Warn("WebView2 process failed: " + e.ProcessFailedKind);
            Status?.Invoke("Proses WebView2 bermasalah. Memuat ulang...");
            try
            {
                _core.Reload();
            }
            catch (Exception exception)
            {
                Log.Error("ProcessFailed reload failed", exception);
                NavigationFailed?.Invoke("ProcessFailed: " + e.ProcessFailedKind);
            }
        }

        private static bool IsAssessmentOrigin(string uri)
        {
            try
            {
                var host = new Uri(uri).Host;
                return string.Equals(host, "ara-assessment.com", StringComparison.OrdinalIgnoreCase) ||
                       host.EndsWith(".ara-assessment.com", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _webView.Dispose();
            }
            catch
            {
            }
        }
    }
}

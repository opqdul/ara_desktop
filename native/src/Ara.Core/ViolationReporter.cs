using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ara.Core
{
    public sealed class ViolationReporter : IDisposable
    {
        private readonly HttpClient _client;

        public ViolationReporter()
        {
            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(6)
            };
        }

        public async Task ReportAsync(
            ExamContext context,
            string violationType,
            string violationLabel,
            string description,
            Func<string, Task<string>> cookieProvider)
        {
            if (context == null || !context.HasIdentity)
            {
                return;
            }

            var baseUrl = (context.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (baseUrl.Length == 0)
            {
                return;
            }

            try
            {
                var cookieHeader = cookieProvider != null
                    ? await cookieProvider(baseUrl).ConfigureAwait(false)
                    : string.Empty;

                var values = new[]
                {
                    new[] { "userID", context.UserId ?? string.Empty },
                    new[] { "examID", context.ExamId ?? string.Empty },
                    new[] { "classID", context.ClassId ?? string.Empty },
                    new[] { "responseID", context.ResponseId ?? string.Empty },
                    new[] { "violation_type", violationType ?? "app_exit" },
                    new[] { "violation_label", violationLabel ?? "Keluar dari Aplikasi" },
                    new[] { "description", description ?? string.Empty },
                    new[] { "source", "ara_desktop_windows" },
                    new[] { "platform", PlatformDescription() }
                };

                using (var content = new FormUrlEncodedContent(Flatten(values)))
                using (var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/Violation/record"))
                {
                    request.Content = content;
                    request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
                    if (!string.IsNullOrWhiteSpace(cookieHeader))
                    {
                        request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                    }

                    using (var response = await _client.SendAsync(request).ConfigureAwait(false))
                    {
                        await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            Log.Warn("Violation fallback HTTP " + (int)response.StatusCode + " for " + violationType);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warn("Violation fallback failed: " + exception.GetType().Name);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> Flatten(
            string[][] values)
        {
            foreach (var pair in values)
            {
                yield return new System.Collections.Generic.KeyValuePair<string, string>(pair[0], pair[1] ?? string.Empty);
            }
        }

        private static string PlatformDescription()
        {
            var version = NativeMethods.GetWindowsVersion();
            var architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            return string.Format(
                "Windows {0}.{1} build {2} ({3})",
                version.Major,
                version.Minor,
                version.Build,
                architecture);
        }
    }
}

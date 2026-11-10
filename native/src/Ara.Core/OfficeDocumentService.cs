using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ara.Core
{
    public enum OfficeDocumentKind
    {
        Word,
        Excel,
        PowerPoint
    }

    public static class OfficeDocumentService
    {
        private static readonly Dictionary<string, OfficeDocumentKind> ExtensionKinds =
            new Dictionary<string, OfficeDocumentKind>(StringComparer.OrdinalIgnoreCase)
            {
                { "doc", OfficeDocumentKind.Word },
                { "docx", OfficeDocumentKind.Word },
                { "xls", OfficeDocumentKind.Excel },
                { "xlsx", OfficeDocumentKind.Excel },
                { "ppt", OfficeDocumentKind.PowerPoint },
                { "pptx", OfficeDocumentKind.PowerPoint }
            };

        private static readonly Dictionary<string, string> ExtensionMimeTypes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "doc", "application/msword" },
                { "docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                { "xls", "application/vnd.ms-excel" },
                { "xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
                { "ppt", "application/vnd.ms-powerpoint" },
                { "pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" }
            };

        private static readonly Dictionary<string, string> MimeExtensions = BuildMimeExtensions();

        public static string ExtensionOf(string nameOrPath)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath))
            {
                return null;
            }

            var value = nameOrPath.Split('?')[0].Split('#')[0];
            var slash = value.LastIndexOf('/');
            var backslash = value.LastIndexOf('\\');
            var separator = Math.Max(slash, backslash);
            if (separator >= 0)
            {
                value = value.Substring(separator + 1);
            }

            var dot = value.LastIndexOf('.');
            if (dot <= 0 || dot == value.Length - 1)
            {
                return null;
            }

            return value.Substring(dot + 1).ToLowerInvariant();
        }

        public static string NormalizeMimeType(string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                return null;
            }

            var value = mimeType.Split(';')[0].Trim().ToLowerInvariant();
            return value.Length == 0 ? null : value;
        }

        public static OfficeDocumentKind? DetectKind(string fileName, Uri url, string mimeType)
        {
            var fromName = ExtensionOf(fileName);
            if (fromName != null && ExtensionKinds.ContainsKey(fromName))
            {
                return ExtensionKinds[fromName];
            }

            if (url != null)
            {
                var fromUrl = ExtensionOf(url.AbsolutePath);
                if (fromUrl != null && ExtensionKinds.ContainsKey(fromUrl))
                {
                    return ExtensionKinds[fromUrl];
                }
            }

            var normalized = NormalizeMimeType(mimeType);
            string extension;
            if (normalized != null && MimeExtensions.TryGetValue(normalized, out extension))
            {
                return ExtensionKinds[extension];
            }

            return null;
        }

        public static bool IsOfficeDocumentUrl(Uri url)
        {
            if (url == null)
            {
                return false;
            }

            var scheme = url.Scheme.ToLowerInvariant();
            if (scheme != "http" && scheme != "https")
            {
                return false;
            }

            var extension = ExtensionOf(url.AbsolutePath);
            return extension != null && ExtensionKinds.ContainsKey(extension);
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "dokumen";
            }

            var cleaned = Regex.Replace(name, @"[\\/:*?""<>|\x00-\x1F]", "_");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            cleaned = cleaned.TrimStart('.').Trim();
            if (cleaned.Length == 0)
            {
                return "dokumen";
            }

            const int maxLength = 120;
            if (cleaned.Length > maxLength)
            {
                var extension = ExtensionOf(cleaned);
                var suffix = extension == null ? string.Empty : "." + extension;
                cleaned = cleaned.Substring(0, maxLength - suffix.Length) + suffix;
            }

            return cleaned;
        }

        public static string FileNameFromContentDisposition(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return null;
            }

            var extended = Regex.Match(
                header,
                @"filename\*\s*=\s*([^']*)'[^']*'([^;]+)",
                RegexOptions.IgnoreCase);
            if (extended.Success)
            {
                var raw = extended.Groups[2].Value.Trim().Trim('"');
                try
                {
                    return Uri.UnescapeDataString(raw);
                }
                catch
                {
                    return raw;
                }
            }

            var quoted = Regex.Match(header, @"filename\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (quoted.Success && quoted.Groups[1].Value.Trim().Length > 0)
            {
                return quoted.Groups[1].Value.Trim();
            }

            var plain = Regex.Match(header, @"filename\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            if (plain.Success && plain.Groups[1].Value.Trim().Length > 0)
            {
                return plain.Groups[1].Value.Trim();
            }

            return null;
        }

        public static string ResolveFileName(string suggested, string contentDisposition, Uri url, string mimeType)
        {
            string candidate = null;
            if (!string.IsNullOrWhiteSpace(suggested))
            {
                candidate = suggested.Trim();
            }

            if (string.IsNullOrWhiteSpace(candidate))
            {
                candidate = FileNameFromContentDisposition(contentDisposition);
            }

            if (string.IsNullOrWhiteSpace(candidate) && url != null)
            {
                var segments = url.Segments;
                if (segments.Length > 0)
                {
                    var last = segments[segments.Length - 1].Trim('/');
                    if (last.Length > 0)
                    {
                        try
                        {
                            candidate = Uri.UnescapeDataString(last);
                        }
                        catch
                        {
                            candidate = last;
                        }
                    }
                }
            }

            var name = SanitizeFileName(candidate ?? "dokumen");
            if (ExtensionOf(name) == null)
            {
                var normalized = NormalizeMimeType(mimeType);
                string extension;
                if (normalized != null && MimeExtensions.TryGetValue(normalized, out extension))
                {
                    name = name + "." + extension;
                }
            }

            return name;
        }

        public static string MimeTypeFor(string fileName, string fallback)
        {
            var extension = ExtensionOf(fileName);
            if (extension != null && ExtensionMimeTypes.ContainsKey(extension))
            {
                return ExtensionMimeTypes[extension];
            }

            var normalized = NormalizeMimeType(fallback);
            if (normalized != null && normalized != "application/octet-stream")
            {
                return normalized;
            }

            return "*/*";
        }

        public static void CleanupOldDownloads()
        {
            try
            {
                if (!Directory.Exists(AppConfig.DownloadsRoot))
                {
                    return;
                }

                var cutoff = DateTime.Now.AddHours(-24);
                foreach (var directory in Directory.GetDirectories(AppConfig.DownloadsRoot))
                {
                    try
                    {
                        if (Directory.GetLastWriteTime(directory) < cutoff)
                        {
                            Directory.Delete(directory, true);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        public static async Task<OfficeOpenResult> DownloadAndOpenAsync(
            Uri url,
            string suggestedFileName,
            string mimeType,
            string cookieHeader,
            string userAgent)
        {
            if (url == null)
            {
                return OfficeOpenResult.Fail("Dokumen tidak valid.");
            }

            var scheme = url.Scheme.ToLowerInvariant();
            if (scheme != "http" && scheme != "https")
            {
                return OfficeOpenResult.Fail("Dokumen ini tidak dapat diunduh.");
            }

            CleanupOldDownloads();
            Directory.CreateDirectory(AppConfig.DownloadsRoot);

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            };

            using (var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(2)
            })
            {
                if (!string.IsNullOrWhiteSpace(userAgent))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
                }

                var current = url;
                var originalHost = url.Host;

                for (var hop = 0; hop <= 5; hop++)
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, current))
                    {
                        if (!string.IsNullOrWhiteSpace(cookieHeader) &&
                            string.Equals(current.Host, originalHost, StringComparison.OrdinalIgnoreCase))
                        {
                            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                        }

                        using (var response = await client.SendAsync(
                            request,
                            HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                        {
                            var status = (int)response.StatusCode;
                            if (status >= 300 && status < 400)
                            {
                                var location = response.Headers.Location;
                                if (location == null)
                                {
                                    return OfficeOpenResult.Fail("Server mengembalikan redirect tanpa tujuan.");
                                }

                                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                                continue;
                            }

                            if (!response.IsSuccessStatusCode)
                            {
                                return OfficeOpenResult.Fail("Gagal mengunduh dokumen (HTTP " + status + ").");
                            }

                            var responseMime = response.Content.Headers.ContentType != null
                                ? response.Content.Headers.ContentType.MediaType
                                : null;
                            var disposition = response.Content.Headers.ContentDisposition != null
                                ? response.Content.Headers.ContentDisposition.ToString()
                                : null;
                            var fileName = ResolveFileName(
                                suggestedFileName,
                                disposition,
                                current,
                                responseMime ?? mimeType);
                            var kind = DetectKind(fileName, current, responseMime ?? mimeType);
                            if (!kind.HasValue)
                            {
                                return OfficeOpenResult.Fail(
                                    "Selama ujian, hanya dokumen Word, Excel, dan PowerPoint yang dapat dibuka.");
                            }

                            var folder = Path.Combine(
                                AppConfig.DownloadsRoot,
                                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
                            Directory.CreateDirectory(folder);
                            var path = Path.Combine(folder, fileName);

                            using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (var target = File.Create(path))
                            {
                                await source.CopyToAsync(target).ConfigureAwait(false);
                            }

                            return OfficeLauncher.OpenDocument(path, kind.Value);
                        }
                    }
                }
            }

            return OfficeOpenResult.Fail("Terlalu banyak redirect saat mengunduh dokumen.");
        }

        private static Dictionary<string, string> BuildMimeExtensions()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in ExtensionMimeTypes)
            {
                result[pair.Value] = pair.Key;
            }
            return result;
        }
    }
}

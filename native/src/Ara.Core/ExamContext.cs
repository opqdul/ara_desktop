using System;
using System.Collections.Generic;

namespace Ara.Core
{
    public sealed class ExamContext
    {
        public string BaseUrl { get; set; }
        public string UserId { get; set; }
        public string ExamId { get; set; }
        public string ClassId { get; set; }
        public string ResponseId { get; set; }

        public ExamContext()
        {
            BaseUrl = string.Empty;
            UserId = string.Empty;
            ExamId = string.Empty;
            ClassId = string.Empty;
            ResponseId = string.Empty;
        }

        public bool HasIdentity
        {
            get { return !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(UserId); }
        }

        public void UpdateFrom(IDictionary<string, object> values)
        {
            if (values == null)
            {
                return;
            }

            BaseUrl = Read(values, "baseUrl", BaseUrl);
            UserId = Read(values, "userID", UserId);
            ExamId = Read(values, "examID", ExamId);
            ClassId = Read(values, "classID", ClassId);
            ResponseId = Read(values, "responseID", ResponseId);

            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                BaseUrl = ResolveBaseUrl(string.Empty);
            }
        }

        public static string ResolveBaseUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return string.Empty;
            }

            var origin = uri.GetLeftPart(UriPartial.Authority);
            var path = uri.AbsolutePath ?? string.Empty;

            if (path.StartsWith("/new_ara", StringComparison.OrdinalIgnoreCase))
            {
                return origin + "/new_ara";
            }

            if (path.StartsWith("/assess", StringComparison.OrdinalIgnoreCase))
            {
                return origin + "/assess";
            }

            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var first = parts[0].ToLowerInvariant();
                switch (first)
                {
                    case "login":
                    case "home":
                    case "exam":
                    case "user":
                    case "violation":
                    case "dashboard":
                    case "result":
                        return origin;
                }

                return origin + "/" + parts[0];
            }

            return origin;
        }

        private static string Read(IDictionary<string, object> values, string key, string fallback)
        {
            object value;
            if (!values.TryGetValue(key, out value) || value == null)
            {
                return fallback ?? string.Empty;
            }

            var text = value.ToString().Trim();
            return text.Length == 0 ? (fallback ?? string.Empty) : text;
        }
    }
}

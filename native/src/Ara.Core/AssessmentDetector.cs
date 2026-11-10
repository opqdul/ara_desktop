using System;
using System.IO;

namespace Ara.Core
{
    public sealed class AssessmentDetector
    {
        private static readonly Lazy<string> Script = new Lazy<string>(LoadScript);

        public static string InjectedScript
        {
            get { return Script.Value; }
        }

        public AssessmentMode EvaluateUrl(string url)
        {
            Uri parsed;
            string path;
            if (Uri.TryCreate(url, UriKind.Absolute, out parsed))
            {
                path = parsed.AbsolutePath ?? string.Empty;
            }
            else
            {
                path = url ?? string.Empty;
            }

            path = path.ToLowerInvariant();

            if (path.Length == 0 ||
                path == "/" ||
                path == "/new_ara" ||
                path == "/new_ara/" ||
                path == "/assess" ||
                path == "/assess/")
            {
                return AssessmentMode.Normal;
            }

            foreach (var fragment in AppConfig.NormalRouteFragments)
            {
                if (path.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                {
                    return AssessmentMode.Normal;
                }
            }

            foreach (var fragment in AppConfig.ExamRouteFragments)
            {
                if (path.IndexOf(fragment, StringComparison.Ordinal) >= 0)
                {
                    return AssessmentMode.Exam;
                }
            }

            return AssessmentMode.Normal;
        }

        public AssessmentMode EvaluateMode(string mode)
        {
            switch ((mode ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "strict":
                case "essay":
                case "exam":
                    return AssessmentMode.Exam;
                default:
                    return AssessmentMode.Normal;
            }
        }

        private static string LoadScript()
        {
            const string resourceName = "Ara.Core.Resources.AssessmentDetector.js";
            using (var stream = typeof(AssessmentDetector).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Log.Error("AssessmentDetector.js resource is missing.");
                    return string.Empty;
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}

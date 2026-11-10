using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Ara.Core
{
    public static class Log
    {
        private static readonly object Sync = new object();
        private static bool _initialized;
        private static string _logPath;

        public static string LogDirectory
        {
            get { return AppConfig.LogDirectory; }
        }

        public static void Initialize()
        {
            lock (Sync)
            {
                if (_initialized)
                {
                    return;
                }

                Directory.CreateDirectory(AppConfig.LogDirectory);
                CleanupOldLogs();
                _logPath = Path.Combine(
                    AppConfig.LogDirectory,
                    "ara-desktop-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
                _initialized = true;
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(string message, Exception exception)
        {
            Write("ERROR", message + " :: " + exception);
        }

        private static void Write(string level, string message)
        {
            Initialize();
            var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) +
                       " [" + level + "] " + (message ?? string.Empty);
            Debug.WriteLine(line);

            try
            {
                lock (Sync)
                {
                    RotateIfNeeded();
                    File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never take the app down.
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(_logPath))
                {
                    return;
                }

                var info = new FileInfo(_logPath);
                if (info.Length <= 10 * 1024 * 1024)
                {
                    return;
                }

                var oldPath = _logPath + ".old";
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }
                File.Move(_logPath, oldPath);
            }
            catch
            {
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-7);
                foreach (var file in Directory.GetFiles(AppConfig.LogDirectory, "ara-desktop-*.log*"))
                {
                    try
                    {
                        if (File.GetLastWriteTime(file) < cutoff)
                        {
                            File.Delete(file);
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
    }
}

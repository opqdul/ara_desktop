using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Ara.Core
{
    public sealed class OfficeOpenResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }

        public OfficeOpenResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public static OfficeOpenResult Ok(string message)
        {
            return new OfficeOpenResult(true, message);
        }

        public static OfficeOpenResult Fail(string message)
        {
            return new OfficeOpenResult(false, message);
        }
    }

    public static class OfficeLauncher
    {
        public static string DisplayName(OfficeDocumentKind kind)
        {
            switch (kind)
            {
                case OfficeDocumentKind.Word: return "Microsoft Word";
                case OfficeDocumentKind.Excel: return "Microsoft Excel";
                case OfficeDocumentKind.PowerPoint: return "Microsoft PowerPoint";
                default: return "Office";
            }
        }

        public static OfficeOpenResult OpenApp(OfficeDocumentKind kind)
        {
            var executable = FindExecutable(kind);
            if (string.IsNullOrWhiteSpace(executable))
            {
                return OfficeOpenResult.Fail(
                    DisplayName(kind) + " tidak ditemukan. Pastikan Microsoft Office, WPS Office, atau LibreOffice terinstal.");
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = true
                });
                return OfficeOpenResult.Ok("Membuka " + DisplayName(kind) + "...");
            }
            catch (Exception exception)
            {
                Log.Warn("Office launch failed: " + exception.GetType().Name);
                return OfficeOpenResult.Fail("Gagal membuka " + DisplayName(kind) + ".");
            }
        }

        public static OfficeOpenResult OpenDocument(string path, OfficeDocumentKind kind)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return OfficeOpenResult.Fail("Dokumen tidak ditemukan.");
            }

            var executable = FindExecutable(kind);
            try
            {
                if (!string.IsNullOrWhiteSpace(executable))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = "\"" + path + "\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                return OfficeOpenResult.Ok("Membuka dokumen di " + DisplayName(kind) + "...");
            }
            catch (Exception exception)
            {
                Log.Warn("Office document open failed: " + exception.GetType().Name);
                return OfficeOpenResult.Fail("Gagal membuka dokumen di " + DisplayName(kind) + ".");
            }
        }

        public static string FindExecutable(OfficeDocumentKind kind)
        {
            var candidates = new List<string>();
            switch (kind)
            {
                case OfficeDocumentKind.Word:
                    candidates.AddRange(new[] { "winword.exe", "wps.exe" });
                    break;
                case OfficeDocumentKind.Excel:
                    candidates.AddRange(new[] { "excel.exe", "et.exe" });
                    break;
                case OfficeDocumentKind.PowerPoint:
                    candidates.AddRange(new[] { "powerpnt.exe", "wpp.exe" });
                    break;
            }

            foreach (var candidate in candidates)
            {
                var fromRegistry = FindInAppPaths(candidate);
                if (!string.IsNullOrWhiteSpace(fromRegistry))
                {
                    return fromRegistry;
                }
            }

            foreach (var candidate in CandidatePaths(kind))
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string FindInAppPaths(string executable)
        {
            var keys = new[]
            {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + executable,
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" + executable
            };

            foreach (var key in keys)
            {
                try
                {
                    var value = Registry.GetValue(key, string.Empty, null) as string;
                    if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
                    {
                        return value;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static IEnumerable<string> CandidatePaths(OfficeDocumentKind kind)
        {
            var roots = new List<string>();
            AddRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            AddRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));

            var officeNames = new[] { "WINWORD.EXE", "EXCEL.EXE", "POWERPNT.EXE" };
            var officeExe = kind == OfficeDocumentKind.Word
                ? officeNames[0]
                : (kind == OfficeDocumentKind.Excel ? officeNames[1] : officeNames[2]);

            foreach (var root in roots)
            {
                foreach (var version in new[] { "root\\Office16", "Office16", "Office15", "Office14", "Office12" })
                {
                    yield return Path.Combine(root, "Microsoft Office", version, officeExe);
                }

                if (kind == OfficeDocumentKind.Word)
                {
                    yield return Path.Combine(root, "Kingsoft", "WPS Office", "office6", "wps.exe");
                }
                else if (kind == OfficeDocumentKind.Excel)
                {
                    yield return Path.Combine(root, "Kingsoft", "WPS Office", "office6", "et.exe");
                }
                else
                {
                    yield return Path.Combine(root, "Kingsoft", "WPS Office", "office6", "wpp.exe");
                }

                yield return Path.Combine(root, "LibreOffice", "program", "soffice.exe");
            }
        }

        private static void AddRoot(ICollection<string> roots, string root)
        {
            if (!string.IsNullOrWhiteSpace(root) && !roots.Contains(root))
            {
                roots.Add(root);
            }
        }
    }
}

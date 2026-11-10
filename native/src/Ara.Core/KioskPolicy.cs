using System;

namespace Ara.Core
{
    public sealed class KioskPolicyEvaluation
    {
        public bool Allowed { get; private set; }
        public bool IsEmergency { get; private set; }
        public string Reason { get; private set; }

        private KioskPolicyEvaluation(bool allowed, bool emergency, string reason)
        {
            Allowed = allowed;
            IsEmergency = emergency;
            Reason = reason ?? string.Empty;
        }

        public static KioskPolicyEvaluation Allow(string reason)
        {
            return new KioskPolicyEvaluation(true, false, reason);
        }

        public static KioskPolicyEvaluation AllowEmergency(string reason)
        {
            return new KioskPolicyEvaluation(true, true, reason);
        }

        public static KioskPolicyEvaluation Block(string reason)
        {
            return new KioskPolicyEvaluation(false, false, reason);
        }
    }

    public static class KioskPolicy
    {
        public static KioskPolicyEvaluation Evaluate(
            AssessmentMode mode,
            string processExe,
            string className,
            string windowTitle,
            bool isOwnProcess)
        {
            if (isOwnProcess)
            {
                return KioskPolicyEvaluation.Allow("Own process");
            }

            if (mode == AssessmentMode.Normal)
            {
                return KioskPolicyEvaluation.Allow("Normal mode");
            }

            var exe = (processExe ?? string.Empty).Trim().ToLowerInvariant();
            var cls = className ?? string.Empty;
            var title = windowTitle ?? string.Empty;

            if (exe.Length == 0)
            {
                if (AppConfig.ShellClasses.Contains(cls))
                {
                    return KioskPolicyEvaluation.Allow("System shell infrastructure");
                }

                return KioskPolicyEvaluation.Block(
                    "Aplikasi sistem atau elevated (" + cls + ") diblokir! " +
                    "Aplikasi dengan hak administrator tidak diizinkan selama ujian.");
            }

            if (exe == "explorer.exe")
            {
                if (cls == "CabinetWClass" || cls == "ExploreWClass")
                {
                    return KioskPolicyEvaluation.Block(
                        "File Explorer diblokir! Anda tidak diizinkan membuka folder/file selama sesi ujian.");
                }

                if (cls == "#32770" &&
                    (title.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     title.IndexOf("jalankan", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return KioskPolicyEvaluation.Block(
                        "Kotak dialog Run diblokir selama sesi ujian!");
                }

                return KioskPolicyEvaluation.Allow("Windows shell infrastructure");
            }

            if (AppConfig.OfficeAllowlist.Contains(exe))
            {
                return KioskPolicyEvaluation.Allow("Exam whitelist");
            }

            if (AppConfig.ShellProcesses.Contains(exe))
            {
                return KioskPolicyEvaluation.Allow("Windows shell process");
            }

            if (AppConfig.EmergencyProcesses.Contains(exe))
            {
                return KioskPolicyEvaluation.AllowEmergency("Emergency Task Manager");
            }

            return KioskPolicyEvaluation.Block(
                "Aplikasi \"" + exe + "\" diblokir! Pada sesi ujian, hanya Microsoft Word, Excel, dan PowerPoint yang diizinkan.");
        }
    }
}

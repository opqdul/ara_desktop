namespace Ara.Desktop.UI
{
    public static class Strings
    {
        public const string AppName = "ARA Assessment";
        public const string Utilities = "Utilities";
        public const string Refresh = "Refresh";
        public const string ExamBadge = "MODE UJIAN AKTIF";
        public const string Loading = "Memuat portal asesmen...";
        public const string Preparing = "Menyiapkan tampilan web...";
        public const string Reloading = "Memuat ulang halaman...";

        public const string ErrorTitle = "Gagal Membuka Halaman ARA Assessment";
        public const string ErrorInitTitle = "Gagal Menyiapkan Tampilan Web";
        public const string Retry = "Coba Lagi";
        public const string OpenLogs = "Buka Folder Log";
        public const string CopyDiagnostics = "Salin Diagnostik";
        public const string Exit = "Keluar";

        public const string SupervisorTitle = "Panel Pengawas";
        public const string SupervisorPin = "Masukkan PIN Pengawas";
        public const string SupervisorUnlock = "Buka Panel";
        public const string SupervisorWrongPin = "PIN salah.";
        public const string SupervisorNotConfigured = "Panel pengawas belum dikonfigurasi pada build ini.";
        public const string SupervisorLocked = "Terlalu banyak percobaan. Coba lagi dalam {0} detik.";
        public const string SupervisorReady = "Akses pengawas terbuka.";

        public const string ModeNormal = "Mode Normal (Buka Kunci)";
        public const string ModeExam = "Mode Ujian (Kunci)";
        public const string ModeAuto = "Mode Otomatis (Ikuti Portal)";
        public const string OpenWord = "Microsoft Word";
        public const string OpenExcel = "Microsoft Excel";
        public const string OpenPowerPoint = "Microsoft PowerPoint";
        public const string ReloadPage = "Reload Halaman";
        public const string ExitApp = "Keluar Aplikasi";
        public const string Close = "Tutup";
        public const string ConfirmExit = "Keluar dari ARA Assessment?";
        public const string ConfirmClear = "Hapus cache sesi WebView2 dan muat ulang?";

        public const string CloseBlocked = "Ujian sedang berlangsung! Aplikasi tidak dapat ditutup.";
        public const string UnauthorizedApp = "Aplikasi \"{0}\" diblokir selama ujian.";
        public const string EmergencyTaskManager = "Task Manager terdeteksi dan diizinkan sebagai jalur darurat.";
        public const string OfficeMissing = "Microsoft Office tidak ditemukan. Pastikan Word, Excel, atau PowerPoint terinstal.";
        public const string OfficeOpening = "Membuka dokumen...";
        public const string DownloadBlocked = "Selama ujian, hanya dokumen Word, Excel, dan PowerPoint yang dapat diunduh.";
        public const string DownloadFailed = "Gagal mengunduh dokumen. Periksa koneksi internet lalu coba lagi.";
        public const string ExternalBlocked = "Membuka aplikasi lain diblokir selama ujian.";
        public const string NotAvailable = "Fitur ini tidak tersedia.";
        public const string LogFolderMissing = "Folder log belum tersedia.";
        public const string DiagnosticsCopied = "Diagnostik disalin ke clipboard.";

        public const string ErrorCapability = "Perangkat ini belum memenuhi persyaratan ARA Assessment.";
        public const string ErrorWebView = "Microsoft Edge WebView2 Runtime tidak ditemukan atau terlalu lama. Jalankan installer ulang atau hubungi pengawas.";
        public const string ErrorNavigation = "Gagal memuat portal asesmen. Periksa koneksi internet lalu coba lagi.";
        public const string ErrorServer = "Server menolak permintaan. Coba lagi atau hubungi pengawas.";
        public const string ErrorInit = "Tampilan web tidak dapat dibuat. Periksa instalasi WebView2 lalu coba lagi.";
        public const string ErrorUnexpected = "Terjadi kesalahan yang tidak terduga. Hubungi pengawas dan sertakan file log.";
    }
}

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Ara.Core
{
    public struct WindowsVersion
    {
        public int Major;
        public int Minor;
        public int Build;
        public int ServicePackMajor;
    }

    public static class NativeMethods
    {
        public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        public const uint WM_SYSCOMMAND = 0x0112;
        public const uint SC_MINIMIZE = 0xF020;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE = 9;
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public delegate void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventProc lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint flags);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint access, bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(
            IntPtr process,
            uint flags,
            StringBuilder exeName,
            ref uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("ntdll.dll", CharSet = CharSet.Unicode)]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEXW versionInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RTL_OSVERSIONINFOEXW
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        public static WindowsVersion GetWindowsVersion()
        {
            var version = new WindowsVersion();
            try
            {
                var info = new RTL_OSVERSIONINFOEXW();
                info.dwOSVersionInfoSize = (uint)Marshal.SizeOf(typeof(RTL_OSVERSIONINFOEXW));
                var status = RtlGetVersion(ref info);
                if (status == 0)
                {
                    version.Major = (int)info.dwMajorVersion;
                    version.Minor = (int)info.dwMinorVersion;
                    version.Build = (int)info.dwBuildNumber;
                    version.ServicePackMajor = info.wServicePackMajor;
                    return version;
                }
            }
            catch
            {
            }

            var fallback = Environment.OSVersion.Version;
            version.Major = fallback.Major;
            version.Minor = fallback.Minor;
            version.Build = fallback.Build;
            return version;
        }

        [DllImport("d3d11.dll", ExactSpelling = true)]
        private static extern int D3D11CreateDevice(
            IntPtr adapter,
            int driverType,
            IntPtr software,
            uint flags,
            IntPtr featureLevels,
            uint featureLevelCount,
            uint sdkVersion,
            out IntPtr device,
            out int featureLevel,
            out IntPtr context);

        public static int GetD3DFeatureLevel()
        {
            var hardware = TryCreateD3DDevice(1);
            if (hardware > 0)
            {
                return hardware;
            }

            return TryCreateD3DDevice(5);
        }

        private static int TryCreateD3DDevice(int driverType)
        {
            IntPtr device = IntPtr.Zero;
            IntPtr context = IntPtr.Zero;
            try
            {
                int featureLevel;
                var result = D3D11CreateDevice(
                    IntPtr.Zero,
                    driverType,
                    IntPtr.Zero,
                    0x20,
                    IntPtr.Zero,
                    0,
                    7,
                    out device,
                    out featureLevel,
                    out context);
                if (result >= 0)
                {
                    return featureLevel;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
            finally
            {
                if (context != IntPtr.Zero)
                {
                    Marshal.Release(context);
                }
                if (device != IntPtr.Zero)
                {
                    Marshal.Release(device);
                }
            }
        }

        public static string GetProcessExecutable(uint processId)
        {
            if (processId == 0)
            {
                return string.Empty;
            }

            var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (handle == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                var buffer = new StringBuilder(1024);
                uint size = (uint)buffer.Capacity;
                if (!QueryFullProcessImageName(handle, 0, buffer, ref size))
                {
                    return string.Empty;
                }

                var fullPath = buffer.ToString();
                var slash = fullPath.LastIndexOf('\\');
                return slash >= 0 ? fullPath.Substring(slash + 1) : fullPath;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        public static string GetWindowClassName(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return string.Empty;
            }

            var buffer = new StringBuilder(256);
            return GetClassName(hwnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : string.Empty;
        }

        public static string GetWindowTitle(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return string.Empty;
            }

            var buffer = new StringBuilder(512);
            return GetWindowText(hwnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : string.Empty;
        }
    }
}

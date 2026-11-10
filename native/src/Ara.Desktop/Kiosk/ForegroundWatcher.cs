using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Ara.Core;

namespace Ara.Desktop.Kiosk
{
    public sealed class ForegroundWatcher : IDisposable
    {
        private readonly IntPtr _ownHwnd;
        private readonly Control _uiControl;
        private readonly Action<IntPtr, string, KioskPolicyEvaluation> _onPolicy;
        private readonly uint _ownPid;
        private readonly Timer _timer;
        private readonly NativeMethods.WinEventProc _hookProc;
        private readonly Dictionary<string, DateTime> _lastNotify = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private IntPtr _hook;
        private bool _running;

        public ForegroundWatcher(
            IntPtr ownHwnd,
            Control uiControl,
            Action<IntPtr, string, KioskPolicyEvaluation> onPolicy)
        {
            _ownHwnd = ownHwnd;
            _uiControl = uiControl;
            _onPolicy = onPolicy;
            _ownPid = NativeMethods.GetCurrentProcessId();
            _hookProc = OnWinEvent;
            _timer = new Timer { Interval = 250 };
            _timer.Tick += (sender, args) => Evaluate();
        }

        public void Start()
        {
            if (_running)
            {
                return;
            }

            _lastNotify.Clear();
            _running = true;
            _hook = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _hookProc,
                0,
                0,
                NativeMethods.WINEVENT_OUTOFCONTEXT);
            _timer.Start();
            Evaluate();
        }

        public void Stop()
        {
            _running = false;
            _timer.Stop();
            if (_hook != IntPtr.Zero)
            {
                NativeMethods.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
        }

        private void OnWinEvent(
            IntPtr hook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint eventThread,
            uint eventTime)
        {
            if (_running && eventType == NativeMethods.EVENT_SYSTEM_FOREGROUND)
            {
                Evaluate();
            }
        }

        private void Evaluate()
        {
            if (!_running)
            {
                return;
            }

            try
            {
                var hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                uint processId;
                NativeMethods.GetWindowThreadProcessId(hwnd, out processId);
                var isOwn = processId == _ownPid || processId == 0;
                var exe = NativeMethods.GetProcessExecutable(processId);
                var className = NativeMethods.GetWindowClassName(hwnd);
                var title = NativeMethods.GetWindowTitle(hwnd);

                var evaluation = KioskPolicy.Evaluate(
                    AssessmentMode.Exam,
                    exe,
                    className,
                    title,
                    isOwn);

                if (evaluation.Allowed && !evaluation.IsEmergency)
                {
                    return;
                }

                if (evaluation.IsEmergency)
                {
                    if (ShouldNotify("emergency:" + exe, TimeSpan.FromSeconds(30)))
                    {
                        Notify(hwnd, exe, evaluation);
                    }
                    return;
                }

                NativeMethods.PostMessage(
                    hwnd,
                    NativeMethods.WM_SYSCOMMAND,
                    new IntPtr(NativeMethods.SC_MINIMIZE),
                    IntPtr.Zero);
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MINIMIZE);
                ForceForeground();

                if (ShouldNotify("block:" + exe, TimeSpan.FromSeconds(10)))
                {
                    Notify(hwnd, exe, evaluation);
                }
            }
            catch (Exception exception)
            {
                Log.Warn("Foreground watcher error: " + exception.GetType().Name);
            }
        }

        private void ForceForeground()
        {
            if (_ownHwnd == IntPtr.Zero || !NativeMethods.IsWindow(_ownHwnd))
            {
                return;
            }

            var foreground = NativeMethods.GetForegroundWindow();
            var currentThread = NativeMethods.GetCurrentThreadId();
            uint foregroundPid;
            var foregroundThread = foreground != IntPtr.Zero
                ? NativeMethods.GetWindowThreadProcessId(foreground, out foregroundPid)
                : 0;

            var attached = false;
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                attached = NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
            }

            try
            {
                NativeMethods.SetWindowPos(
                    _ownHwnd,
                    NativeMethods.HWND_TOPMOST,
                    0,
                    0,
                    0,
                    0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
                NativeMethods.SetWindowPos(
                    _ownHwnd,
                    NativeMethods.HWND_NOTOPMOST,
                    0,
                    0,
                    0,
                    0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
                NativeMethods.ShowWindow(_ownHwnd, 5);
                NativeMethods.BringWindowToTop(_ownHwnd);
                NativeMethods.SetForegroundWindow(_ownHwnd);
            }
            finally
            {
                if (attached)
                {
                    NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
                }
            }
        }

        private bool ShouldNotify(string key, TimeSpan cooldown)
        {
            DateTime last;
            var now = DateTime.UtcNow;
            if (_lastNotify.TryGetValue(key, out last) && now - last < cooldown)
            {
                return false;
            }

            _lastNotify[key] = now;
            return true;
        }

        private void Notify(IntPtr hwnd, string exe, KioskPolicyEvaluation evaluation)
        {
            if (_uiControl != null && _uiControl.InvokeRequired)
            {
                _uiControl.BeginInvoke(new Action(() =>
                {
                    if (_onPolicy != null)
                    {
                        _onPolicy(hwnd, exe, evaluation);
                    }
                }));
                return;
            }

            if (_onPolicy != null)
            {
                _onPolicy(hwnd, exe, evaluation);
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ara.Desktop.Kiosk
{
    public sealed class WindowGuard
    {
        private const int WmClose = 0x0010;
        private const int WmSysCommand = 0x0112;
        private const int WmNcLButtonDown = 0x00A1;
        private const int WmNcLButtonDblClk = 0x00A3;
        private const int WmNcRButtonDown = 0x00A4;
        private const int WmNcRButtonUp = 0x00A5;

        private const int ScSize = 0xF000;
        private const int ScMove = 0xF010;
        private const int ScMinimize = 0xF020;
        private const int ScMaximize = 0xF030;
        private const int ScClose = 0xF060;
        private const int ScKeyMenu = 0xF100;
        private const int ScRestore = 0xF120;
        private const int ScTaskList = 0xF130;
        private const int ScHotKey = 0xF150;

        private readonly Form _form;
        private readonly Action _closeAttempt;
        private Rectangle _normalBounds;
        private FormWindowState _normalState = FormWindowState.Normal;

        public bool ExamMode { get; private set; }
        public bool AllowExit { get; set; }

        public WindowGuard(Form form, Action closeAttempt)
        {
            _form = form;
            _closeAttempt = closeAttempt;
        }

        public void SetExam(bool exam)
        {
            if (exam)
            {
                if (!ExamMode)
                {
                    _normalBounds = _form.Bounds;
                    _normalState = _form.WindowState;
                }

                ExamMode = true;
                _form.MaximizeBox = false;
                _form.MinimizeBox = false;
                _form.ControlBox = false;
                _form.TopMost = false;
                _form.WindowState = FormWindowState.Maximized;
                return;
            }

            ExamMode = false;
            _form.ControlBox = true;
            _form.MaximizeBox = true;
            _form.MinimizeBox = true;
            _form.TopMost = false;
            _form.WindowState = FormWindowState.Normal;

            if (_normalBounds.Width > 0 && _normalBounds.Height > 0)
            {
                _form.Bounds = _normalBounds;
            }

            if (_normalState == FormWindowState.Maximized)
            {
                _form.WindowState = FormWindowState.Maximized;
            }
        }

        public bool HandleMessage(ref Message message)
        {
            if (!ExamMode)
            {
                return false;
            }

            switch (message.Msg)
            {
                case WmClose:
                    FireCloseAttempt();
                    return true;

                case WmSysCommand:
                    var command = message.WParam.ToInt32() & 0xFFF0;
                    if (command == ScClose)
                    {
                        FireCloseAttempt();
                        return true;
                    }

                    if (command == ScMinimize ||
                        command == ScMaximize ||
                        command == ScRestore ||
                        command == ScMove ||
                        command == ScSize ||
                        command == ScKeyMenu ||
                        command == ScTaskList ||
                        command == ScHotKey)
                    {
                        return true;
                    }
                    break;

                case WmNcLButtonDown:
                case WmNcLButtonDblClk:
                case WmNcRButtonDown:
                case WmNcRButtonUp:
                    return true;
            }

            return false;
        }

        public bool HandleFormClosing(FormClosingEventArgs args)
        {
            if (!ExamMode || AllowExit)
            {
                return false;
            }

            args.Cancel = true;
            FireCloseAttempt();
            return true;
        }

        private void FireCloseAttempt()
        {
            if (_closeAttempt != null)
            {
                _closeAttempt();
            }
        }
    }
}

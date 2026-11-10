using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Ara.Core;

namespace Ara.Desktop.UI
{
    public sealed class ErrorForm : Form
    {
        private readonly string _diagnostics;

        public ErrorForm(string title, string message, string diagnostics, bool canRetry)
        {
            _diagnostics = diagnostics ?? string.Empty;

            Text = Strings.AppName;
            Icon = SystemIcons.Error;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(30, 30, 46);
            ForeColor = Color.White;
            ClientSize = new Size(680, 430);

            var titleLabel = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(24, 22),
                Size = new Size(630, 34)
            };

            var icon = new PictureBox
            {
                Image = SystemIcons.Error.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(24, 74),
                Size = new Size(42, 42)
            };

            var messageLabel = new Label
            {
                Text = message,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                Location = new Point(82, 74),
                Size = new Size(570, 230)
            };

            var logLabel = new Label
            {
                Text = "Log: " + AppConfig.LogDirectory,
                ForeColor = Color.Gray,
                Font = new Font("Consolas", 8),
                AutoSize = false,
                Location = new Point(24, 318),
                Size = new Size(630, 20)
            };

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Location = new Point(24, 356),
                Size = new Size(630, 48),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            var exitButton = MakeButton(Strings.Exit, Color.FromArgb(155, 40, 40));
            exitButton.DialogResult = DialogResult.Abort;

            var copyButton = MakeButton(Strings.CopyDiagnostics, Color.FromArgb(55, 55, 75));
            copyButton.Click += (sender, args) => CopyDiagnostics();

            var logsButton = MakeButton(Strings.OpenLogs, Color.FromArgb(55, 55, 75));
            logsButton.Click += (sender, args) => OpenLogs();

            var retryButton = MakeButton(Strings.Retry, Color.FromArgb(0, 120, 190));
            retryButton.DialogResult = DialogResult.Retry;
            retryButton.Visible = canRetry;

            buttons.Controls.Add(exitButton);
            buttons.Controls.Add(copyButton);
            buttons.Controls.Add(logsButton);
            if (canRetry)
            {
                buttons.Controls.Add(retryButton);
                AcceptButton = retryButton;
            }
            else
            {
                AcceptButton = exitButton;
            }
            CancelButton = exitButton;

            Controls.Add(titleLabel);
            Controls.Add(icon);
            Controls.Add(messageLabel);
            Controls.Add(logLabel);
            Controls.Add(buttons);
        }

        private static Button MakeButton(string text, Color color)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = false,
                Size = new Size(150, 36),
                Margin = new Padding(8, 0, 0, 0),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private void CopyDiagnostics()
        {
            try
            {
                Clipboard.SetText(_diagnostics + Environment.NewLine + "Log: " + AppConfig.LogDirectory);
            }
            catch
            {
            }
        }

        private static void OpenLogs()
        {
            try
            {
                Process.Start("explorer.exe", AppConfig.LogDirectory);
            }
            catch
            {
            }
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using Ara.Core;

namespace Ara.Desktop.UI
{
    public enum SupervisorAction
    {
        None,
        Normal,
        Exam,
        Auto,
        Word,
        Excel,
        PowerPoint,
        Reload,
        OpenLogs,
        CopyDiagnostics,
        Exit
    }

    public sealed class SupervisorForm : Form
    {
        private readonly SupervisorAuth _auth;
        private readonly string _diagnostics;
        private readonly Panel _pinPanel;
        private readonly Panel _actionsPanel;
        private readonly TextBox _pinBox;
        private readonly Button _unlockButton;
        private readonly Label _status;
        private readonly Timer _lockoutTimer;

        public SupervisorAction SelectedAction { get; private set; }

        public SupervisorForm(SupervisorAuth auth, string diagnostics)
        {
            _auth = auth;
            _diagnostics = diagnostics ?? string.Empty;
            SelectedAction = SupervisorAction.None;

            Text = Strings.SupervisorTitle;
            Icon = SystemIcons.Shield;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(30, 30, 46);
            ForeColor = Color.White;
            ClientSize = new Size(560, 520);

            _pinPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _actionsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };

            var title = new Label
            {
                Text = Strings.SupervisorTitle,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(28, 24)
            };

            var prompt = new Label
            {
                Text = Strings.SupervisorPin,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(30, 86)
            };

            _pinBox = new TextBox
            {
                UseSystemPasswordChar = true,
                Location = new Point(30, 116),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(45, 45, 62),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _unlockButton = new Button
            {
                Text = Strings.SupervisorUnlock,
                Location = new Point(344, 114),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(0, 120, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _unlockButton.FlatAppearance.BorderSize = 0;
            _unlockButton.Click += (sender, args) => TryUnlock();

            _status = new Label
            {
                ForeColor = Color.Orange,
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Location = new Point(30, 158),
                Size = new Size(500, 44)
            };

            var closePin = new Button
            {
                Text = Strings.Close,
                DialogResult = DialogResult.Cancel,
                Location = new Point(410, 210),
                Size = new Size(120, 34),
                BackColor = Color.FromArgb(55, 55, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            closePin.FlatAppearance.BorderSize = 0;

            _pinPanel.Controls.Add(title);
            _pinPanel.Controls.Add(prompt);
            _pinPanel.Controls.Add(_pinBox);
            _pinPanel.Controls.Add(_unlockButton);
            _pinPanel.Controls.Add(_status);
            _pinPanel.Controls.Add(closePin);

            var actionsTitle = new Label
            {
                Text = Strings.SupervisorTitle,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(28, 20)
            };

            var actionsHint = new Label
            {
                Text = Strings.SupervisorReady,
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(30, 56)
            };

            var grid = new TableLayoutPanel
            {
                Location = new Point(26, 88),
                Size = new Size(508, 380),
                ColumnCount = 2,
                RowCount = 6,
                BackColor = Color.Transparent
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (var i = 0; i < 6; i++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            }

            AddAction(grid, Strings.ModeNormal, SupervisorAction.Normal, Color.FromArgb(45, 95, 70), 0, 0);
            AddAction(grid, Strings.ModeExam, SupervisorAction.Exam, Color.FromArgb(120, 50, 50), 1, 0);
            AddAction(grid, Strings.ModeAuto, SupervisorAction.Auto, Color.FromArgb(55, 75, 110), 0, 1);
            AddAction(grid, Strings.OpenWord, SupervisorAction.Word, Color.FromArgb(43, 87, 154), 1, 1);
            AddAction(grid, Strings.OpenExcel, SupervisorAction.Excel, Color.FromArgb(33, 115, 70), 0, 2);
            AddAction(grid, Strings.OpenPowerPoint, SupervisorAction.PowerPoint, Color.FromArgb(180, 70, 40), 1, 2);
            AddAction(grid, Strings.ReloadPage, SupervisorAction.Reload, Color.FromArgb(60, 60, 85), 0, 3);
            AddAction(grid, Strings.OpenLogs, SupervisorAction.OpenLogs, Color.FromArgb(60, 60, 85), 1, 3);
            AddAction(grid, Strings.CopyDiagnostics, SupervisorAction.CopyDiagnostics, Color.FromArgb(60, 60, 85), 0, 4);
            AddAction(grid, Strings.ExitApp, SupervisorAction.Exit, Color.FromArgb(135, 40, 40), 1, 4);

            var close = new Button
            {
                Text = Strings.Close,
                DialogResult = DialogResult.Cancel,
                Location = new Point(410, 472),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(55, 55, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            close.FlatAppearance.BorderSize = 0;

            _actionsPanel.Controls.Add(actionsTitle);
            _actionsPanel.Controls.Add(actionsHint);
            _actionsPanel.Controls.Add(grid);
            _actionsPanel.Controls.Add(close);

            Controls.Add(_pinPanel);
            Controls.Add(_actionsPanel);
            AcceptButton = _unlockButton;
            CancelButton = closePin;

            _lockoutTimer = new Timer { Interval = 1000 };
            _lockoutTimer.Tick += (sender, args) => UpdateLockoutState();
            _lockoutTimer.Start();

            if (!_auth.IsConfigured)
            {
                _pinBox.Enabled = false;
                _unlockButton.Enabled = false;
                _status.Text = Strings.SupervisorNotConfigured;
                _status.ForeColor = Color.OrangeRed;
            }
            else
            {
                UpdateLockoutState();
            }
        }

        private void AddAction(
            TableLayoutPanel grid,
            string text,
            SupervisorAction action,
            Color color,
            int column,
            int row)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (sender, args) =>
            {
                if (action == SupervisorAction.Exit)
                {
                    var confirm = MessageBox.Show(
                        this,
                        Strings.ConfirmExit,
                        Strings.AppName,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (confirm != DialogResult.Yes)
                    {
                        return;
                    }
                }

                SelectedAction = action;
                DialogResult = DialogResult.OK;
                Close();
            };
            grid.Controls.Add(button, column, row);
        }

        private void TryUnlock()
        {
            if (_auth.IsLockedOut)
            {
                UpdateLockoutState();
                return;
            }

            if (!_auth.Verify(_pinBox.Text))
            {
                _status.ForeColor = Color.OrangeRed;
                _status.Text = _auth.IsLockedOut
                    ? string.Format(Strings.SupervisorLocked, _auth.RemainingLockoutSeconds)
                    : Strings.SupervisorWrongPin;
                _pinBox.Clear();
                _pinBox.Focus();
                return;
            }

            _lockoutTimer.Stop();
            _pinPanel.Visible = false;
            _actionsPanel.Visible = true;
            _actionsPanel.BringToFront();
            AcceptButton = null;
        }

        private void UpdateLockoutState()
        {
            if (!_auth.IsConfigured)
            {
                return;
            }

            if (_auth.IsLockedOut)
            {
                _unlockButton.Enabled = false;
                _status.ForeColor = Color.OrangeRed;
                _status.Text = string.Format(Strings.SupervisorLocked, _auth.RemainingLockoutSeconds);
                return;
            }

            _unlockButton.Enabled = true;
            if (string.IsNullOrWhiteSpace(_status.Text))
            {
                _status.ForeColor = Color.Gray;
                _status.Text = string.Empty;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lockoutTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

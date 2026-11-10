using System;
using System.Drawing;
using System.Windows.Forms;
using Ara.Core;

namespace Ara.Desktop.UI
{
    public sealed class TopBar : Panel
    {
        private readonly Label _badge;

        public event EventHandler RefreshClicked;
        public event EventHandler SupervisorRequested;
        public event Action<OfficeDocumentKind> OfficeRequested;

        public TopBar()
        {
            Dock = DockStyle.Top;
            Height = 32;
            BackColor = Color.FromArgb(43, 43, 43);
            Padding = new Padding(8, 0, 8, 0);

            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            var brand = new Label
            {
                Text = Strings.AppName,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 16, 0)
            };

            var utilities = MakeButton(Strings.Utilities);
            var menu = BuildOfficeMenu();
            utilities.Click += (sender, args) => menu.Show(utilities, new Point(0, utilities.Height));

            var refresh = MakeButton(Strings.Refresh);
            refresh.Click += (sender, args) =>
            {
                var handler = RefreshClicked;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            left.Controls.Add(brand);
            left.Controls.Add(utilities);
            left.Controls.Add(refresh);

            _badge = new Label
            {
                Text = Strings.ExamBadge,
                Dock = DockStyle.Right,
                Width = 170,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(125, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Visible = false
            };
            _badge.DoubleClick += (sender, args) =>
            {
                var handler = SupervisorRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            };

            Controls.Add(_badge);
            Controls.Add(left);
        }

        public void SetExam(bool exam)
        {
            _badge.Visible = exam;
        }

        private ContextMenuStrip BuildOfficeMenu()
        {
            var menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                ShowImageMargin = false
            };

            menu.Items.Add(MakeMenuItem(Strings.OpenWord, OfficeDocumentKind.Word));
            menu.Items.Add(MakeMenuItem(Strings.OpenExcel, OfficeDocumentKind.Excel));
            menu.Items.Add(MakeMenuItem(Strings.OpenPowerPoint, OfficeDocumentKind.PowerPoint));
            return menu;
        }

        private ToolStripMenuItem MakeMenuItem(string text, OfficeDocumentKind kind)
        {
            var item = new ToolStripMenuItem(text)
            {
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(8, 4, 8, 4)
            };
            item.Click += (sender, args) =>
            {
                var handler = OfficeRequested;
                if (handler != null)
                {
                    handler(kind);
                }
            };
            return item;
        }

        private static Button MakeButton(string text)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 24,
                Margin = new Padding(0, 1, 8, 0),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 65, 65);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 80);
            return button;
        }
    }
}

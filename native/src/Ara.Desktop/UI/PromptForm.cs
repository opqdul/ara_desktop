using System.Drawing;
using System.Windows.Forms;

namespace Ara.Desktop.UI
{
    public sealed class PromptForm : Form
    {
        private readonly TextBox _textBox;

        public PromptForm(string message, string defaultValue)
        {
            Text = Strings.AppName;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(30, 30, 46);
            ForeColor = Color.White;
            ClientSize = new Size(520, 220);

            var prompt = new Label
            {
                Text = string.IsNullOrWhiteSpace(message) ? "Masukkan input:" : message,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 10),
                AutoSize = false,
                Location = new Point(22, 20),
                Size = new Size(475, 70)
            };

            _textBox = new TextBox
            {
                Text = defaultValue ?? string.Empty,
                Location = new Point(22, 96),
                Size = new Size(475, 26),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(45, 45, 62),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(322, 150),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(0, 120, 190),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            ok.FlatAppearance.BorderSize = 0;

            var cancel = new Button
            {
                Text = "Batal",
                DialogResult = DialogResult.Cancel,
                Location = new Point(414, 150),
                Size = new Size(83, 32),
                BackColor = Color.FromArgb(55, 55, 75),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cancel.FlatAppearance.BorderSize = 0;

            Controls.Add(prompt);
            Controls.Add(_textBox);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        public string Value
        {
            get { return _textBox.Text; }
        }
    }
}

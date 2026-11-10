using System.Drawing;
using System.Windows.Forms;

namespace Ara.Desktop.UI
{
    public sealed class LoadingForm : Form
    {
        private readonly Label _status;

        public LoadingForm(Icon icon)
        {
            Text = Strings.AppName;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(43, 43, 43);
            ClientSize = new Size(480, 220);

            var picture = new PictureBox
            {
                Location = new Point(36, 52),
                Size = new Size(72, 72),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            if (icon != null)
            {
                picture.Image = icon.ToBitmap();
            }

            var title = new Label
            {
                Text = Strings.AppName,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(128, 56)
            };

            _status = new Label
            {
                Text = Strings.Loading,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(130, 92)
            };

            var progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Location = new Point(130, 124),
                Size = new Size(310, 16)
            };

            var footer = new Label
            {
                Text = "PT ARA Indonesia",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8),
                AutoSize = true,
                Location = new Point(130, 160)
            };

            Controls.Add(picture);
            Controls.Add(title);
            Controls.Add(_status);
            Controls.Add(progress);
            Controls.Add(footer);
        }

        public void SetStatus(string text)
        {
            _status.Text = text ?? string.Empty;
        }
    }
}

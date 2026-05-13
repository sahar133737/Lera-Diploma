using System.Drawing;
using System.Windows.Forms;
using Lera_Diploma.UI;

namespace Lera_Diploma.Forms
{
    /// <summary>Базовое модальное окно: цветная шапка и область содержимого.</summary>
    public class MaterialModalForm : Form
    {
        public Panel Body { get; }

        public MaterialModalForm(string title, Color? headerColor = null, int width = 480, int height = 360, string helpModuleKey = null)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
            ClientSize = new Size(width, height);
            Font = new Font("Segoe UI", 9f);

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = headerColor ?? UiTheme.Primary,
                Padding = new Padding(12, 0, 12, 0)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var lbl = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(4, 14, 0, 0)
            };
            header.Controls.Add(lbl, 0, 0);
            if (!string.IsNullOrEmpty(helpModuleKey))
            {
                var h = new Button
                {
                    Text = "Справка",
                    AutoSize = true,
                    Anchor = AnchorStyles.Right,
                    Margin = new Padding(8, 10, 0, 10),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand,
                    TabStop = false
                };
                h.FlatAppearance.BorderColor = Color.FromArgb(200, 255, 255, 255);
                h.FlatAppearance.BorderSize = 1;
                h.BackColor = Color.FromArgb(40, 255, 255, 255);
                h.Click += (_, __) =>
                {
                    using (var hf = new HelpForm(helpModuleKey))
                        hf.ShowDialog(this);
                };
                header.Controls.Add(h, 1, 0);
            }

            Body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                AutoScroll = true,
                BackColor = UiTheme.PageBackground
            };

            Controls.Add(Body);
            Controls.Add(header);
        }

        public static Button CreateDialogButton(string text, DialogResult result, bool primary)
        {
            var b = new Button { Text = text, DialogResult = result, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            if (primary)
                MaterialStyle.StyleToolbarButton(b, true);
            else
                MaterialStyle.StyleOutlinedButton(b);
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MaterialStyle.ApplyButtonMinWidth(b);
            return b;
        }
    }
}

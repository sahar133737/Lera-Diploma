using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Lera_Diploma.UI;

namespace Lera_Diploma.Forms
{
    /// <summary>HTML-справка по модулю (локальные файлы из папки Help).</summary>
    public sealed class HelpForm : Form
    {
        public HelpForm(string moduleKey)
        {
            Text = "Справка — " + moduleKey;
            Size = new Size(760, 600);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = true;
            BackColor = UiTheme.PageBackground;

            var browser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true,
                IsWebBrowserContextMenuEnabled = true
            };
            Controls.Add(browser);

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var modPath = Path.Combine(baseDir, "Help", moduleKey + ".html");
            if (File.Exists(modPath))
            {
                browser.Navigate(new Uri(modPath));
                return;
            }

            var guide = Path.Combine(baseDir, "Help", "guide.html");
            if (File.Exists(guide))
            {
                browser.Navigate(guide + "#" + moduleKey);
                return;
            }

            var def = Path.Combine(baseDir, "Help", "default.html");
            if (File.Exists(def))
                browser.Navigate(new Uri(def));
            else
                browser.DocumentText = "<html><body style=\"font-family:Segoe UI;padding:16px;\"><p>Файлы справки не найдены. Скопируйте папку Help рядом с приложением.</p></body></html>";
        }
    }
}

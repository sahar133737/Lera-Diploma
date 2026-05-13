using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Lera_Diploma.Forms;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class BackupUserControl : UserControl
    {
        private static string GetDefaultBackupFilePath()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "FinanceDubrovsky",
                "Backup");
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                // ignore; user may pick another folder
            }

            return Path.Combine(dir, "FinanceDubrovsky.bak");
        }

        private readonly TextBox _txtPath = new TextBox();
        private readonly Button _btnDefaultPath = new Button { Text = "Путь по умолчанию" };
        private readonly Button _btnSaveAs = new Button { Text = "Сохранить как…" };
        private readonly Button _btnOpenFile = new Button { Text = "Открыть…" };
        private readonly Button _btnBackup = new Button { Text = "Создать .bak" };
        private readonly Button _btnRestore = new Button { Text = "Восстановить из .bak" };
        private readonly BackupService _svc = new BackupService();

        public BackupUserControl()
        {
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);

            MaterialStyle.StyleToolbarButton(_btnDefaultPath);
            MaterialStyle.StyleToolbarButton(_btnSaveAs);
            MaterialStyle.StyleToolbarButton(_btnOpenFile);
            MaterialStyle.StyleToolbarButton(_btnBackup, true);
            MaterialStyle.StyleToolbarButton(_btnRestore);

            var pathToolbar = ModuleToolbar.CreateDockTopToolbar();
            pathToolbar.Controls.Add(_btnDefaultPath);
            pathToolbar.Controls.Add(_btnSaveAs);
            pathToolbar.Controls.Add(_btnOpenFile);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lbl = new Label
            {
                Text = "Путь к файлу резервной копии (.bak):",
                AutoSize = true,
                ForeColor = UiTheme.TextPrimary,
                Margin = new Padding(0, 8, 0, 4)
            };
            _txtPath.Text = GetDefaultBackupFilePath();
            _txtPath.Dock = DockStyle.Fill;
            MaterialStyle.StyleTextBox(_txtPath);

            var rowBtns = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 4, 0, 8) };
            rowBtns.Controls.Add(_btnBackup);
            rowBtns.Controls.Add(_btnRestore);

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Каталог должен быть доступен учётной записи службы SQL Server (часто это не «Рабочий стол» пользователя). «Сохранить как…» — выбрать файл для новой резервной копии; «Открыть…» — выбрать существующий .bak для восстановления.",
                ForeColor = UiTheme.Muted,
                Padding = new Padding(0, 8, 0, 0)
            };

            layout.Controls.Add(pathToolbar, 0, 0);
            layout.Controls.Add(lbl, 0, 1);
            layout.Controls.Add(_txtPath, 0, 2);
            layout.Controls.Add(rowBtns, 0, 3);
            layout.Controls.Add(hint, 0, 4);

            Controls.Add(layout);

            _btnDefaultPath.Click += (_, __) => { _txtPath.Text = GetDefaultBackupFilePath(); };
            _btnSaveAs.Click += BtnSaveAs_Click;
            _btnOpenFile.Click += BtnOpenFile_Click;
            _btnBackup.Click += BtnBackup_Click;
            _btnRestore.Click += BtnRestore_Click;
        }

        private void BtnSaveAs_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog
            {
                Filter = "Резервная копия SQL Server|*.bak|Все файлы|*.*",
                FileName = string.IsNullOrWhiteSpace(_txtPath.Text) ? "FinanceDubrovsky.bak" : Path.GetFileName(_txtPath.Text),
                InitialDirectory = TryGetDirectory(_txtPath.Text),
                Title = "Куда сохранить резервную копию"
            })
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    _txtPath.Text = dlg.FileName;
            }
        }

        private void BtnOpenFile_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "Резервная копия SQL Server|*.bak|Все файлы|*.*",
                FileName = string.IsNullOrWhiteSpace(_txtPath.Text) ? "" : Path.GetFileName(_txtPath.Text),
                InitialDirectory = TryGetDirectory(_txtPath.Text),
                Title = "Выберите файл .bak"
            })
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    _txtPath.Text = dlg.FileName;
            }
        }

        private static string TryGetDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                var dir = Path.GetDirectoryName(path);
                return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            catch
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
        }

        private void BtnBackup_Click(object sender, EventArgs e)
        {
            var msg = _svc.TryBackupToFile(_txtPath.Text.Trim(), out var err);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Резерв", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                new AuditService().Write(CurrentUserContext.UserId, "Backup", "Database", _txtPath.Text, null);
                MessageBox.Show(FindForm(), msg, "Резерв", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(FindForm(), "Восстановление перезапишет текущую базу. Продолжить?", "Внимание", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            var msg = _svc.TryRestoreFromFile(_txtPath.Text.Trim(), out var err);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Восстановление", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                new AuditService().Write(CurrentUserContext.UserId, "Restore", "Database", _txtPath.Text, null);
                MessageBox.Show(FindForm(), msg, "Восстановление", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

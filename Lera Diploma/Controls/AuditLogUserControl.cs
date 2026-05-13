using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Lera_Diploma.Forms;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class AuditLogUserControl : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnExcel = new Button { Text = "Excel" };

        public AuditLogUserControl()
        {
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            var top = ModuleToolbar.CreateDockTopToolbar();
            MaterialStyle.StyleToolbarButton(_btnRefresh, true);
            MaterialStyle.StyleToolbarButton(_btnExcel);
            top.Controls.Add(_btnRefresh);
            top.Controls.Add(_btnExcel);
            MaterialStyle.StyleDataGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToOrderColumns = true;
            _grid.AllowUserToAddRows = false;
            _grid.ReadOnly = true;
            var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            host.Controls.Add(top, 0, 0);
            host.Controls.Add(_grid, 0, 1);
            Controls.Add(host);
            _btnRefresh.Click += (_, __) =>
            {
                Reload();
                UserFeedback.Info(FindForm(), "Список записей журнала обновлён.", "Журнал аудита");
            };
            _btnExcel.Click += BtnExcel_Click;
            Load += AuditLogUserControl_Load;
        }

        private void AuditLogUserControl_Load(object sender, EventArgs e)
        {
            var canExport = RolePermissionService.HasPermission(ModuleKeys.ReportsExport);
            _btnExcel.Enabled = canExport;
            Reload();
        }

        private void BtnExcel_Click(object sender, EventArgs e)
        {
            if (_grid.DataSource is not DataTable table || table.Rows.Count == 0)
            {
                MessageBox.Show(FindForm(), "Нет данных для экспорта.", "Журнал аудита", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "Журнал_аудита.xlsx" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                    return;
                try
                {
                    ReportExportService.ExportToExcel(table, "Журнал аудита", dlg.FileName);
                    MessageBox.Show(FindForm(), "Файл сохранён.", "Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(FindForm(), ex.Message, "Excel", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Reload()
        {
            using (var db = new Lera_Diploma.Data.FinancialDbContext())
            {
                var rows = db.AuditLogs.OrderByDescending(x => x.CreatedAtUtc).Take(500).ToList().Select(x => new
                {
                    x.CreatedAtUtc,
                    x.UserId,
                    x.Action,
                    x.EntityType,
                    x.EntityKey,
                    x.Details
                }).ToList();
                var table = EnumerableToDataTable.FromRows(rows);
                _grid.DataSource = table;
                GridHeaderMap.Apply(_grid, "audit", "UserId");
            }
        }
    }
}

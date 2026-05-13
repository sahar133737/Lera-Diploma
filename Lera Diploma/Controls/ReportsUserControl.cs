using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Lera_Diploma.Data;
using Lera_Diploma.Forms;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class ReportsUserControl : UserControl
    {
        private readonly ComboBox _cbReport = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
        private readonly Label _lblAccount = new Label { Text = "Счёт:", AutoSize = true, Margin = new Padding(12, 10, 4, 0), ForeColor = UiTheme.TextPrimary };
        private readonly DateTimePicker _dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly ComboBox _cbAccount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnAdd = new Button { Text = "Добавить" };
        private readonly Button _btnEdit = new Button { Text = "Изменить" };
        private readonly Button _btnDelete = new Button { Text = "Удалить" };
        private readonly Button _btnBuild = new Button { Text = "Сформировать" };
        private readonly Button _btnExcel = new Button { Text = "Excel" };
        private readonly Button _btnPdf = new Button { Text = "PDF" };
        private readonly Button _btnPrint = new Button { Text = "Печать" };
        private readonly DataGridView _grid = new DataGridView();
        private readonly Chart _chartPrimary = new Chart();
        private readonly Chart _chartSecondary = new Chart();
        private DataTable _lastTable;
        private string _lastTitle;

        public ReportsUserControl()
        {
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

            var filters = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 0, 0, 4) };
            filters.Controls.Add(new Label { Text = "Отчёт:", AutoSize = true, Margin = new Padding(0, 10, 4, 0), ForeColor = UiTheme.TextPrimary });
            _cbReport.Items.AddRange(new object[]
            {
                "Карточка счёта: обороты за период",
                "Аналитика оборотов: месяц и контрагент",
                "Исполнение бюджета: свод по статьям",
                "Бюджет в разрезе контрагента, типа документа и статьи"
            });
            _cbReport.SelectedIndex = 0;
            filters.Controls.Add(_cbReport);
            filters.Controls.Add(_lblAccount);
            filters.Controls.Add(_cbAccount);
            filters.Controls.Add(new Label { Text = "С:", AutoSize = true, Margin = new Padding(12, 10, 4, 0), ForeColor = UiTheme.TextPrimary });
            filters.Controls.Add(_dtFrom);
            filters.Controls.Add(new Label { Text = "По:", AutoSize = true, Margin = new Padding(8, 10, 4, 0), ForeColor = UiTheme.TextPrimary });
            filters.Controls.Add(_dtTo);
            filters.Controls.Add(_btnBuild);
            filters.Controls.Add(_btnExcel);
            filters.Controls.Add(_btnPdf);
            filters.Controls.Add(_btnPrint);

            var buttons = ModuleToolbar.CreateDockTopToolbar();
            buttons.Controls.Add(_btnRefresh);
            buttons.Controls.Add(_btnAdd);
            buttons.Controls.Add(_btnEdit);
            buttons.Controls.Add(_btnDelete);

            MaterialStyle.StyleToolbarButton(_btnRefresh);
            MaterialStyle.StyleToolbarButton(_btnAdd);
            MaterialStyle.StyleToolbarButton(_btnEdit);
            MaterialStyle.StyleToolbarButton(_btnDelete);
            MaterialStyle.StyleToolbarButton(_btnBuild, true);
            MaterialStyle.StyleToolbarButton(_btnExcel);
            MaterialStyle.StyleToolbarButton(_btnPdf);
            MaterialStyle.StyleToolbarButton(_btnPrint);
            MaterialStyle.StyleCombo(_cbReport);
            MaterialStyle.StyleCombo(_cbAccount);
            _btnAdd.Enabled = _btnEdit.Enabled = _btnDelete.Enabled = false;

            MaterialStyle.StyleDataGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToOrderColumns = true;

            var chartsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            chartsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            chartsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            var p1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 6, 0) };
            var p2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 0, 0) };
            ConfigureReportChart(_chartPrimary, UiTheme.Success, "Основная диаграмма");
            ConfigureReportChart(_chartSecondary, UiTheme.Info, "Дополнительная диаграмма");
            p1.Controls.Add(_chartPrimary);
            p2.Controls.Add(_chartSecondary);
            chartsPanel.Controls.Add(p1, 0, 0);
            chartsPanel.Controls.Add(p2, 1, 0);

            var gridHost = new Panel { Dock = DockStyle.Fill };
            gridHost.Controls.Add(_grid);

            root.Controls.Add(filters, 0, 0);
            root.Controls.Add(buttons, 0, 1);
            root.Controls.Add(gridHost, 0, 2);
            root.Controls.Add(chartsPanel, 0, 3);

            Controls.Add(root);

            _btnRefresh.Click += (_, __) => Build();
            _btnBuild.Click += (_, __) => Build();
            _btnExcel.Click += ExportExcel;
            _btnPdf.Click += ExportPdf;
            _btnPrint.Click += PrintClick;
            _cbReport.SelectedIndexChanged += (_, __) => SyncAccountFilterVisibility();
            Load += ReportsUserControl_Load;
        }

        private void SyncAccountFilterVisibility()
        {
            var needAccount = _cbReport.SelectedIndex == 0;
            _lblAccount.Visible = needAccount;
            _cbAccount.Visible = needAccount;
        }

        private static void ConfigureReportChart(Chart chart, Color accent, string titleText)
        {
            chart.Dock = DockStyle.Fill;
            chart.BackColor = UiTheme.CardSurface;
            chart.Titles.Clear();
            chart.Titles.Add(new Title(titleText)
            {
                Docking = Docking.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accent
            });
            chart.ChartAreas.Clear();
            var area = new ChartArea("a");
            area.BackColor = UiTheme.CardSurface;
            area.AxisX.LabelStyle.ForeColor = UiTheme.TextPrimary;
            area.AxisY.LabelStyle.ForeColor = UiTheme.TextPrimary;
            area.AxisY.LabelStyle.Format = "N0";
            area.AxisX.Title = "Категория";
            area.AxisY.Title = "Значение";
            area.AxisX.TitleForeColor = UiTheme.TextMuted;
            area.AxisY.TitleForeColor = UiTheme.TextMuted;
            area.AxisX.TitleFont = new Font("Segoe UI", 8.5f);
            area.AxisY.TitleFont = new Font("Segoe UI", 8.5f);
            area.AxisX.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(230, 230, 235);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(230, 230, 235);
            area.AxisX.LabelStyle.Angle = -30;
            chart.ChartAreas.Add(area);
            chart.Series.Clear();
            chart.Series.Add(new Series("s") { ChartType = SeriesChartType.Column, Color = accent });
            chart.Legends.Clear();
        }

        private void ApplyExportPermissions()
        {
            var canExport = RolePermissionService.HasPermission(ModuleKeys.ReportsExport);
            _btnExcel.Enabled = canExport;
            _btnPdf.Enabled = canExport;
            _btnPrint.Enabled = RolePermissionService.HasPermission(ModuleKeys.Reports);
        }

        private void ReportsUserControl_Load(object sender, EventArgs e)
        {
            SyncAccountFilterVisibility();
            ApplyExportPermissions();
            using (var db = new FinancialDbContext())
            {
                foreach (var a in db.Accounts.OrderBy(x => x.Code))
                    _cbAccount.Items.Add(new AccountItem(a.Id, a.Code + " — " + a.Name));
                if (_cbAccount.Items.Count > 0)
                    _cbAccount.SelectedIndex = 0;
            }
            _dtFrom.Value = DateTime.Today.AddMonths(-3);
            _dtTo.Value = DateTime.Today;
        }

        private sealed class AccountItem
        {
            public int Id { get; }
            public string Text { get; }
            public AccountItem(int id, string text) { Id = id; Text = text; }
            public override string ToString() => Text;
        }

        private void Build()
        {
            try
            {
                var rep = new ReportingService();
                if (_cbReport.SelectedIndex == 0)
                {
                    if (!(_cbAccount.SelectedItem is AccountItem acc))
                        return;
                    _lastTitle = "Карточка счёта — обороты за период";
                    _lastTable = rep.ReportAccountTurnover(acc.Id, _dtFrom.Value.Date, _dtTo.Value.Date);
                }
                else if (_cbReport.SelectedIndex == 1)
                {
                    _lastTitle = "Аналитика оборотов по месяцу и контрагенту";
                    _lastTable = rep.ReportEntriesByMonthAndCounterparty(_dtFrom.Value.Date, _dtTo.Value.Date);
                }
                else if (_cbReport.SelectedIndex == 2)
                {
                    _lastTitle = "Исполнение бюджета по статьям";
                    _lastTable = rep.ReportBudgetSummary(_dtFrom.Value.Date, _dtTo.Value.Date);
                }
                else
                {
                    _lastTitle = "Бюджет: контрагент, тип документа и статья (аллокации)";
                    _lastTable = rep.ReportBudgetAllocationsByCounterpartyDocTypeAndArticle(_dtFrom.Value.Date, _dtTo.Value.Date);
                }

                _grid.DataSource = _lastTable;
                PopulateChartsFromTable(_lastTable, _cbReport.SelectedIndex);
                UserFeedback.Info(FindForm(),
                    _lastTable.Rows.Count == 0
                        ? "За выбранный период данных не найдено."
                        : $"Отчёт сформирован. Строк в таблице: {_lastTable.Rows.Count}.",
                    "Отчёт");
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Отчёт", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateChartsFromTable(DataTable table, int reportIndex)
        {
            void Clear(Chart c)
            {
                c.Series["s"].Points.Clear();
                c.Titles[0].Text = "Нет данных";
            }

            if (table == null || table.Rows.Count == 0)
            {
                Clear(_chartPrimary);
                Clear(_chartSecondary);
                return;
            }

            int sumCol = -1;
            for (var i = table.Columns.Count - 1; i >= 0; i--)
            {
                if (table.Columns[i].DataType == typeof(decimal) || table.Columns[i].DataType == typeof(double) || table.Columns[i].DataType == typeof(int))
                {
                    sumCol = i;
                    break;
                }
            }
            if (sumCol < 0)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    if (string.Equals(table.Columns[i].ColumnName, "Сумма", StringComparison.OrdinalIgnoreCase))
                    {
                        sumCol = i;
                        break;
                    }
                }
            }
            if (sumCol < 0)
            {
                Clear(_chartPrimary);
                Clear(_chartSecondary);
                return;
            }

            bool Skip(DataRow r)
            {
                var t = string.Join(" ", r.ItemArray.Select(x => x?.ToString() ?? ""));
                return t.IndexOf("ИТОГО", StringComparison.OrdinalIgnoreCase) >= 0
                    || t.IndexOf("ВСЕГО", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            DataRow[] rows = table.Rows.Cast<DataRow>().Where(r => !Skip(r)).ToArray();
            if (rows.Length == 0)
            {
                Clear(_chartPrimary);
                Clear(_chartSecondary);
                return;
            }

            int labelCol = 0;
            for (var i = 0; i < table.Columns.Count; i++)
            {
                if (i == sumCol)
                    continue;
                if (table.Columns[i].DataType == typeof(string) || table.Columns[i].DataType == typeof(DateTime))
                {
                    labelCol = i;
                    break;
                }
            }

            void FillBar(Chart chart, string title, int maxPoints)
            {
                chart.Titles[0].Text = title;
                chart.Series["s"].Points.Clear();
                chart.Series["s"].ChartType = SeriesChartType.Column;
                var take = rows
                    .Select(r =>
                    {
                        var raw = r[sumCol];
                        decimal v = 0;
                        if (raw != null && raw != DBNull.Value)
                            decimal.TryParse(raw.ToString(), out v);
                        return new { Label = TruncateLabel(r[labelCol]?.ToString() ?? "—"), Value = Math.Abs(v) };
                    })
                    .OrderByDescending(x => x.Value)
                    .Take(maxPoints)
                    .ToList();
                foreach (var p in take)
                    chart.Series["s"].Points.AddXY(p.Label, (double)p.Value);
            }

            if (reportIndex == 0)
            {
                _chartPrimary.Titles[0].Text = "Суммы по датам (топ дней)";
                _chartPrimary.Series["s"].Points.Clear();
                _chartPrimary.Series["s"].ChartType = SeriesChartType.Line;
                var dateCol = table.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(DateTime));
                if (dateCol != null)
                {
                    var byDay = rows
                        .Where(r => r[dateCol] != DBNull.Value)
                        .GroupBy(r => ((DateTime)r[dateCol]).Date)
                        .Select(g => new { Day = g.Key, Sum = g.Sum(x => Convert.ToDecimal(x[sumCol])) })
                        .OrderBy(x => x.Day)
                        .ToList();
                    foreach (var p in byDay)
                        _chartPrimary.Series["s"].Points.AddXY(p.Day.ToString("dd.MM.yy"), (double)p.Sum);
                }
                else
                    FillBar(_chartPrimary, "Топ строк по сумме", 12);

                FillBar(_chartSecondary, "Топ по сумме (строки)", 10);
            }
            else if (reportIndex == 1)
            {
                FillBar(_chartPrimary, "Топ контрагентов по сумме", 12);
                _chartSecondary.Titles[0].Text = "Суммы по месяцам";
                _chartSecondary.Series["s"].Points.Clear();
                _chartSecondary.Series["s"].ChartType = SeriesChartType.Column;
                var monthIdx = table.Columns.Cast<DataColumn>().ToList().FindIndex(c => c.ColumnName.Contains("Месяц"));
                if (monthIdx >= 0)
                {
                    var byMonth = rows.GroupBy(r => r[monthIdx]?.ToString() ?? "")
                        .Select(g => new { Key = g.Key, Sum = g.Sum(x => Convert.ToDecimal(x[sumCol])) })
                        .OrderBy(x => x.Key)
                        .ToList();
                    foreach (var p in byMonth)
                        _chartSecondary.Series["s"].Points.AddXY(TruncateLabel(p.Key, 12), (double)p.Sum);
                }
                else
                    FillBar(_chartSecondary, "Распределение", 10);
            }
            else if (reportIndex == 2)
            {
                var nameCol = table.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.Contains("Наименование") || c.ColumnName.Contains("Статья"))?.Ordinal ?? labelCol;
                labelCol = nameCol;
                FillBar(_chartPrimary, "Топ статей по сумме", 12);
                FillBar(_chartSecondary, "Статьи (сокращённо)", 8);
            }
            else
            {
                FillBar(_chartPrimary, "Топ контрагентов (аллокации бюджета)", 12);
                var typeCol = table.Columns.Cast<DataColumn>().ToList().FindIndex(c => c.ColumnName.Contains("Тип док"));
                if (typeCol >= 0 && typeCol != sumCol)
                {
                    _chartSecondary.Titles[0].Text = "Суммы по типам документов";
                    _chartSecondary.Series["s"].Points.Clear();
                    _chartSecondary.Series["s"].ChartType = SeriesChartType.Column;
                    var byType = rows
                        .GroupBy(r => r[typeCol]?.ToString() ?? "—")
                        .Select(g => new { Key = g.Key, Sum = g.Sum(x => Convert.ToDecimal(x[sumCol])) })
                        .OrderByDescending(x => x.Sum)
                        .Take(12)
                        .ToList();
                    foreach (var p in byType)
                        _chartSecondary.Series["s"].Points.AddXY(TruncateLabel(p.Key, 16), (double)p.Sum);
                }
                else
                    FillBar(_chartSecondary, "Топ строк по сумме", 10);
            }
        }

        private static string TruncateLabel(string s, int max = 18)
        {
            if (string.IsNullOrEmpty(s))
                return "—";
            s = s.Replace("\r", "").Replace("\n", " ");
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }

        private void ExportExcel(object sender, EventArgs e)
        {
            if (_lastTable == null)
            {
                MessageBox.Show(this, "Сначала сформируйте отчёт.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = Sanitize(_lastTitle) + ".xlsx" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                    return;
                try
                {
                    ReportExportService.ExportToExcel(_lastTable, _lastTitle, dlg.FileName);
                    MessageBox.Show(this, "Файл сохранён.", "Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Excel", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportPdf(object sender, EventArgs e)
        {
            if (_lastTable == null)
            {
                MessageBox.Show(this, "Сначала сформируйте отчёт.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = Sanitize(_lastTitle) + ".pdf" })
            {
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK)
                    return;
                try
                {
                    ReportExportService.ExportToPdf(_lastTable, _lastTitle, dlg.FileName);
                    MessageBox.Show(this, "Файл сохранён.", "PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void PrintClick(object sender, EventArgs e)
        {
            if (_lastTable == null)
            {
                MessageBox.Show(this, "Сначала сформируйте отчёт.", "Печать", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                ReportExportService.PrintDataTable(_lastTable, _lastTitle, FindForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Печать", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return string.IsNullOrWhiteSpace(s) ? "report" : s;
        }
    }
}

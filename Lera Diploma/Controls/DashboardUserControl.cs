using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Lera_Diploma.Forms;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class DashboardUserControl : UserControl
    {
        private readonly ComboBox _cbPeriod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        private readonly DateTimePicker _dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        private readonly DateTimePicker _dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 120 };
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly FlowLayoutPanel _kpiHost = new FlowLayoutPanel();
        private readonly Chart _chartLine = new Chart();
        private readonly Chart _chartColumn = new Chart();
        private readonly DataGridView _grid = new DataGridView();
        private readonly ToolTip _chartTip = new ToolTip { ShowAlways = true, AutomaticDelay = 200, AutoPopDelay = 8000 };

        public DashboardUserControl()
        {
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);

            MaterialStyle.StyleToolbarButton(_btnRefresh, true);
            MaterialStyle.StyleCombo(_cbPeriod);
            _cbPeriod.Items.AddRange(new object[]
            {
                "Сегодня", "Неделя", "Месяц", "Квартал", "Год", "Произвольно"
            });
            _cbPeriod.SelectedIndex = 2;
            _cbPeriod.SelectedIndexChanged += (_, __) => OnPeriodPresetChanged();

            var toolbar = ModuleToolbar.CreateDockTopToolbar();
            toolbar.MaximumSize = new Size(0, 120);
            toolbar.Controls.Add(new Label { Text = "Период:", AutoSize = true, Margin = new Padding(0, 10, 6, 0), ForeColor = UiTheme.TextPrimary });
            toolbar.Controls.Add(_cbPeriod);
            toolbar.Controls.Add(new Label { Text = "С:", AutoSize = true, Margin = new Padding(12, 10, 6, 0), ForeColor = UiTheme.TextPrimary });
            toolbar.Controls.Add(_dtFrom);
            toolbar.Controls.Add(new Label { Text = "По:", AutoSize = true, Margin = new Padding(8, 10, 6, 0), ForeColor = UiTheme.TextPrimary });
            toolbar.Controls.Add(_dtTo);
            toolbar.Controls.Add(_btnRefresh);

            _kpiHost.Dock = DockStyle.Fill;
            _kpiHost.Height = 110;
            _kpiHost.MinimumSize = new Size(0, 110);
            _kpiHost.AutoSize = false;
            _kpiHost.FlowDirection = FlowDirection.LeftToRight;
            _kpiHost.WrapContents = true;

            var charts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.MinimumSize = new Size(0, 280);

            ConfigureChart(_chartLine, UiTheme.Success, SeriesChartType.Line, "Оборот по дням (проведённые)", "Дата", "Сумма проводок, ₽");
            ConfigureChart(_chartColumn, UiTheme.Warning, SeriesChartType.Column, "Суммы по типам документов", "Тип документа", "Сумма, ₽");

            var p1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
            p1.Controls.Add(_chartLine);
            var p2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
            p2.Controls.Add(_chartColumn);
            charts.Controls.Add(p1, 0, 0);
            charts.Controls.Add(p2, 1, 0);

            var lblRecent = new Label
            {
                Text = "Последние документы",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                Padding = new Padding(0, 8, 0, 6)
            };

            MaterialStyle.StyleDataGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToOrderColumns = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.CellDoubleClick += Grid_CellDoubleClick;

            var gridPanel = new Panel { Dock = DockStyle.Fill };
            lblRecent.Dock = DockStyle.Top;
            _grid.Dock = DockStyle.Fill;
            gridPanel.Controls.Add(lblRecent);
            gridPanel.Controls.Add(_grid);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 300f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.Controls.Add(toolbar, 0, 0);
            root.Controls.Add(_kpiHost, 0, 1);
            root.Controls.Add(charts, 0, 2);
            root.Controls.Add(gridPanel, 0, 3);

            Controls.Add(root);
            _btnRefresh.Click += (_, __) => Reload();
            Load += DashboardUserControl_Load;

            WireChartHover(_chartLine, true);
            WireChartHover(_chartColumn, false);
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || !RolePermissionService.HasPermission(ModuleKeys.DocumentsEdit))
                return;
            if (!_grid.Columns.Contains("Id"))
                return;
            var v = _grid.Rows[e.RowIndex].Cells["Id"].Value;
            if (v == null || v == DBNull.Value)
                return;
            var id = Convert.ToInt32(v);
            using (var f = new DocumentEditForm(id))
            {
                if (f.ShowDialog(FindForm()) == DialogResult.OK && f.Saved)
                {
                    UserFeedback.Info(FindForm(), "Документ успешно сохранён.", "Главное меню");
                    Reload();
                }
            }
        }

        private void DashboardUserControl_Load(object sender, EventArgs e)
        {
            OnPeriodPresetChanged();
            Reload();
        }

        private static void ConfigureChart(Chart chart, Color headerColor, SeriesChartType type, string title, string xTitle, string yTitle)
        {
            chart.Dock = DockStyle.Fill;
            chart.BackColor = UiTheme.CardSurface;
            chart.MinimumSize = new Size(200, 240);
            chart.Titles.Clear();
            var titleObj = new Title(title)
            {
                Docking = Docking.Top,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = headerColor
            };
            chart.Titles.Add(titleObj);
            chart.BorderlineColor = UiTheme.Divider;
            chart.BorderlineDashStyle = ChartDashStyle.Solid;
            chart.BorderlineWidth = 1;

            chart.ChartAreas.Clear();
            var area = new ChartArea("main");
            area.BackColor = UiTheme.CardSurface;
            area.BorderColor = UiTheme.Divider;
            area.BorderDashStyle = ChartDashStyle.Solid;
            area.BorderWidth = 1;

            area.AxisX.MajorGrid.Enabled = true;
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(230, 230, 235);
            area.AxisY.MajorGrid.Enabled = true;
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(230, 230, 235);

            area.AxisX.LabelStyle.ForeColor = UiTheme.TextPrimary;
            area.AxisY.LabelStyle.ForeColor = UiTheme.TextPrimary;
            area.AxisX.LineColor = UiTheme.Divider;
            area.AxisY.LineColor = UiTheme.Divider;
            area.AxisX.Title = xTitle;
            area.AxisY.Title = yTitle;
            area.AxisX.TitleForeColor = UiTheme.TextMuted;
            area.AxisY.TitleForeColor = UiTheme.TextMuted;
            area.AxisX.TitleFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            area.AxisY.TitleFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            area.AxisY.LabelStyle.Format = "N0";
            area.AxisX.LabelStyle.Angle = type == SeriesChartType.Column ? -20 : -35;
            area.AxisX.Interval = 1;
            area.AxisX.IsMarginVisible = true;

            chart.ChartAreas.Add(area);

            chart.Series.Clear();
            var s = new Series("data")
            {
                ChartType = type,
                Color = headerColor,
                BorderColor = Color.FromArgb(80, headerColor),
                BorderWidth = 1,
                IsValueShownAsLabel = false,
                IsXValueIndexed = true,
                ToolTip = "#VALY{N0} ₽"
            };
            if (type == SeriesChartType.Line)
            {
                s.BorderWidth = 2;
                s.MarkerStyle = MarkerStyle.None;
            }

            chart.Series.Add(s);
            chart.Legends.Clear();
        }

        private void WireChartHover(Chart chart, bool isLineChart)
        {
            chart.MouseMove += (_, e) =>
            {
                var area = chart.ChartAreas[0];
                var s = chart.Series["data"];
                if (s.Points.Count == 0)
                {
                    _chartTip.SetToolTip(chart, "");
                    return;
                }

                for (var i = 0; i < s.Points.Count; i++)
                {
                    s.Points[i].MarkerSize = 0;
                    s.Points[i].MarkerStyle = MarkerStyle.None;
                    s.Points[i].BorderWidth = 1;
                }

                var best = -1;
                double bestDist = 80;
                for (var i = 0; i < s.Points.Count; i++)
                {
                    var px = area.AxisX.ValueToPixelPosition(s.Points[i].XValue);
                    var dist = Math.Abs(px - e.X);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = i;
                    }
                }

                if (best < 0)
                {
                    _chartTip.SetToolTip(chart, "");
                    return;
                }

                var pt = s.Points[best];
                pt.MarkerStyle = MarkerStyle.Circle;
                pt.MarkerSize = isLineChart ? 10 : 0;
                if (!isLineChart)
                    pt.BorderWidth = 2;

                var label = string.IsNullOrEmpty(pt.AxisLabel) ? pt.XValue.ToString("0") : pt.AxisLabel;
                var y = pt.YValues.Length > 0 ? pt.YValues[0] : 0;
                _chartTip.SetToolTip(chart, $"{label}\nСумма: {y:N0} ₽");
            };

            chart.MouseLeave += (_, __) =>
            {
                var s = chart.Series["data"];
                for (var i = 0; i < s.Points.Count; i++)
                {
                    s.Points[i].MarkerSize = 0;
                    s.Points[i].MarkerStyle = MarkerStyle.None;
                    s.Points[i].BorderWidth = 1;
                }

                _chartTip.SetToolTip(chart, "");
            };
        }

        private void OnPeriodPresetChanged()
        {
            var today = DateTime.Today;
            switch (_cbPeriod.SelectedIndex)
            {
                case 0:
                    _dtFrom.Value = today;
                    _dtTo.Value = today;
                    break;
                case 1:
                    _dtFrom.Value = today.AddDays(-6);
                    _dtTo.Value = today;
                    break;
                case 2:
                    _dtFrom.Value = today.AddDays(-29);
                    _dtTo.Value = today;
                    break;
                case 3:
                    _dtFrom.Value = today.AddDays(-89);
                    _dtTo.Value = today;
                    break;
                case 4:
                    _dtFrom.Value = today.AddDays(-364);
                    _dtTo.Value = today;
                    break;
                default:
                    break;
            }
            var custom = _cbPeriod.SelectedIndex == 5;
            _dtFrom.Enabled = custom;
            _dtTo.Enabled = custom;
        }

        private void Reload()
        {
            try
            {
                var from = _dtFrom.Value.Date;
                var to = _dtTo.Value.Date;
                if (to < from)
                {
                    MessageBox.Show(FindForm(), "Дата «По» раньше даты «С».", "Период", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var svc = new DashboardService();
                var kpis = svc.GetKpis(from, to);
                _kpiHost.Controls.Clear();
                foreach (var k in kpis)
                    _kpiHost.Controls.Add(CreateKpiCard(k));

                var daily = svc.GetDailyPostedAmounts(from, to);
                var seriesL = _chartLine.Series["data"];
                seriesL.Points.Clear();
                var ix = 0;
                foreach (var p in daily)
                {
                    seriesL.Points.AddXY(ix++, (double)p.Value);
                    seriesL.Points[seriesL.Points.Count - 1].AxisLabel = p.Key.ToString("dd.MM");
                }

                var byType = svc.GetPostedAmountsByDocType(from, to);
                var seriesC = _chartColumn.Series["data"];
                seriesC.Points.Clear();
                ix = 0;
                foreach (var p in byType)
                {
                    var name = string.IsNullOrEmpty(p.Key) ? "(без типа)" : p.Key;
                    if (name.Length > 22)
                        name = name.Substring(0, 20) + "…";
                    seriesC.Points.AddXY(ix++, (double)p.Value);
                    seriesC.Points[seriesC.Points.Count - 1].AxisLabel = name;
                }

                var recent = svc.GetRecentDocuments(50);
                _grid.DataSource = EnumerableToDataTable.FromRows((System.Collections.IEnumerable)recent);
                GridHeaderMap.Apply(_grid, "dashboard_recent", "Id");
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Главное меню", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Panel CreateKpiCard(DashboardService.KpiRow k)
        {
            var accent = new[] { UiTheme.Warning, UiTheme.Success, UiTheme.Danger, UiTheme.Info };
            var rnd = k.Title.GetHashCode();
            var chip = accent[Math.Abs(rnd) % accent.Length];

            var card = new Panel
            {
                Width = 230,
                Height = 96,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = UiTheme.CardSurface
            };
            var topChip = new Panel { Height = 6, Dock = DockStyle.Top, BackColor = chip };
            var title = new Label
            {
                Text = k.Title,
                ForeColor = UiTheme.TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(12, 14),
                AutoSize = true
            };
            var value = new Label
            {
                Text = k.Value,
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                Location = new Point(12, 34),
                AutoSize = true
            };
            var sub = new Label
            {
                Text = k.Subtitle,
                ForeColor = UiTheme.TextMuted,
                Font = new Font("Segoe UI", 8f),
                Location = new Point(12, 72),
                AutoSize = true
            };
            card.Controls.Add(sub);
            card.Controls.Add(value);
            card.Controls.Add(title);
            card.Controls.Add(topChip);
            card.Paint += (_, e) =>
            {
                var r = card.ClientRectangle;
                r.Width--;
                r.Height--;
                using (var pen = new Pen(UiTheme.Divider))
                    e.Graphics.DrawRectangle(pen, r);
            };
            return card;
        }
    }
}

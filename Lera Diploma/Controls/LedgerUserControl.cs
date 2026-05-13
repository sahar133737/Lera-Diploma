using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Lera_Diploma.Data;
using Lera_Diploma.Forms;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class LedgerUserControl : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _txtSearch = new TextBox { Width = 200 };
        private readonly DateTimePicker _dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly ComboBox _cbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnAdd = new Button { Text = "Добавить" };
        private readonly Button _btnEdit = new Button { Text = "Изменить" };
        private readonly Button _btnDelete = new Button { Text = "Удалить" };
        private readonly ToolTip _tip = new ToolTip();
        private readonly Timer _searchDebounce = new Timer { Interval = 400 };

        public LedgerUserControl()
        {
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);

            MaterialStyle.StyleToolbarButton(_btnRefresh);
            MaterialStyle.StyleToolbarButton(_btnAdd, true);
            MaterialStyle.StyleToolbarButton(_btnEdit);
            MaterialStyle.StyleToolbarButton(_btnDelete);
            MaterialStyle.StyleTextBox(_txtSearch);
            MaterialStyle.StyleCombo(_cbStatus);

            var top = ModuleToolbar.CreateDockTopToolbar();
            top.MinimumSize = new Size(0, 88);
            top.Controls.Add(new Label { Text = "Поиск:", AutoSize = true, Margin = new Padding(0, 10, 4, 0), ForeColor = UiTheme.TextPrimary });
            top.Controls.Add(_txtSearch);
            top.Controls.Add(new Label { Text = "С:", AutoSize = true, Margin = new Padding(12, 10, 4, 0), ForeColor = UiTheme.TextPrimary });
            top.Controls.Add(_dtFrom);
            top.Controls.Add(new Label { Text = "По:", AutoSize = true, Margin = new Padding(8, 10, 4, 0), ForeColor = UiTheme.TextPrimary });
            top.Controls.Add(_dtTo);
            top.Controls.Add(new Label { Text = "Статус док.:", AutoSize = true, Margin = new Padding(12, 10, 4, 0), ForeColor = UiTheme.TextPrimary });
            top.Controls.Add(_cbStatus);

            var row2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            row2.Controls.Add(_btnRefresh);
            row2.Controls.Add(_btnAdd);
            row2.Controls.Add(_btnEdit);
            row2.Controls.Add(_btnDelete);
            top.Controls.Add(row2);

            MaterialStyle.StyleDataGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToOrderColumns = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;

            var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            host.Controls.Add(top, 0, 0);
            host.Controls.Add(_grid, 0, 1);
            Controls.Add(host);

            _btnRefresh.Click += (_, __) => Reload();
            _btnAdd.Click += BtnAdd_Click;
            _btnEdit.Click += BtnEdit_Click;
            _btnDelete.Click += BtnDelete_Click;
            _searchDebounce.Tick += (_, __) =>
            {
                _searchDebounce.Stop();
                Reload();
            };
            _txtSearch.TextChanged += (_, __) =>
            {
                _searchDebounce.Stop();
                _searchDebounce.Start();
            };
            _txtSearch.Leave += (_, __) =>
            {
                _searchDebounce.Stop();
                Reload();
            };
            _cbStatus.SelectedIndexChanged += (_, __) => Reload();
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    BtnEdit_Click(_, EventArgs.Empty);
            };
            DataGridViewSearchHighlighter.Attach(_grid, _txtSearch);
            Load += LedgerUserControl_Load;
        }

        private void LedgerUserControl_Load(object sender, EventArgs e)
        {
            using (var db = new FinancialDbContext())
            {
                _cbStatus.Items.Add(new StatusItem(null, "Все"));
                foreach (var s in db.DocumentStatuses.OrderBy(x => x.Name))
                    _cbStatus.Items.Add(new StatusItem(s.Id, s.Name));
                _cbStatus.SelectedIndex = 0;
            }
            _dtFrom.Value = DateTime.Today.AddMonths(-12);
            _dtTo.Value = DateTime.Today.AddDays(1);
            var canEdit = RolePermissionService.HasPermission(ModuleKeys.DocumentsEdit);
            _btnAdd.Enabled = canEdit;
            _btnEdit.Enabled = canEdit;
            _btnDelete.Enabled = canEdit;
            _tip.SetToolTip(_btnAdd, "Создать новый документ (черновик)");
            _tip.SetToolTip(_btnEdit, "Открыть документ, к которому относится строка");
            _tip.SetToolTip(_btnDelete, "Удалить черновик документа по выбранной строке");
            Reload();
        }

        private sealed class StatusItem
        {
            public int? Id { get; }
            public string Name { get; }
            public StatusItem(int? id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }

        private int? GetDocId()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("FinancialDocumentId"))
                return null;
            var v = _grid.CurrentRow.Cells["FinancialDocumentId"].Value;
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
        }

        private void Reload()
        {
            int? st = null;
            if (_cbStatus.SelectedItem is StatusItem si && si.Id.HasValue)
                st = si.Id.Value;
            var svc = new LedgerService();
            var raw = svc.GetEntriesForGrid(_txtSearch.Text, _dtFrom.Value.Date, _dtTo.Value.Date, st);
            _grid.DataSource = EnumerableToDataTable.FromRows((System.Collections.IEnumerable)raw);
            GridHeaderMap.Apply(_grid, "ledger", "Id", "FinancialDocumentId");
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var f = new DocumentEditForm(null))
            {
                if (f.ShowDialog(FindForm()) == DialogResult.OK && f.Saved)
                {
                    UserFeedback.Info(FindForm(), "Документ успешно сохранён.", "Журнал проводок");
                    Reload();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var id = GetDocId();
            if (!id.HasValue)
                return;
            using (var f = new DocumentEditForm(id.Value))
            {
                if (f.ShowDialog(FindForm()) == DialogResult.OK && f.Saved)
                {
                    UserFeedback.Info(FindForm(), "Документ успешно сохранён.", "Журнал проводок");
                    Reload();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var id = GetDocId();
            if (!id.HasValue)
                return;
            if (MessageBox.Show(FindForm(), "Удалить черновик документа?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var err = new DocumentService().TryDelete(id.Value);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                UserFeedback.Deleted(FindForm(), "Черновик документа");
                Reload();
            }
        }
    }
}

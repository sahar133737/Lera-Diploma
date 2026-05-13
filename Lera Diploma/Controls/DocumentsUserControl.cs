using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Lera_Diploma.Forms;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class DocumentsUserControl : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _txtSearch = new TextBox { Width = 200 };
        private readonly ComboBox _cbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        private readonly DateTimePicker _dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnAdd = new Button { Text = "Добавить" };
        private readonly Button _btnEdit = new Button { Text = "Изменить" };
        private readonly Button _btnDelete = new Button { Text = "Удалить" };
        private readonly Button _btnPost = new Button { Text = "Провести" };
        private readonly Timer _searchDebounce = new Timer { Interval = 400 };

        public DocumentsUserControl()
        {
            BackColor = UiTheme.PageBackground;
            ForeColor = UiTheme.TextPrimary;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);

            MaterialStyle.StyleToolbarButton(_btnRefresh);
            MaterialStyle.StyleToolbarButton(_btnAdd, true);
            MaterialStyle.StyleToolbarButton(_btnEdit);
            MaterialStyle.StyleToolbarButton(_btnDelete);
            MaterialStyle.StyleToolbarButton(_btnPost, true);
            MaterialStyle.StyleTextBox(_txtSearch);
            MaterialStyle.StyleCombo(_cbStatus);

            var panelTop = ModuleToolbar.CreateDockTopToolbar();
            panelTop.MinimumSize = new Size(0, 88);
            panelTop.Controls.Add(new Label { Text = "Поиск:", AutoSize = true, ForeColor = UiTheme.TextPrimary, Margin = new Padding(0, 10, 4, 0) });
            panelTop.Controls.Add(_txtSearch);
            panelTop.Controls.Add(new Label { Text = "Статус:", AutoSize = true, ForeColor = UiTheme.TextPrimary, Margin = new Padding(12, 10, 4, 0) });
            panelTop.Controls.Add(_cbStatus);
            panelTop.Controls.Add(new Label { Text = "С:", AutoSize = true, ForeColor = UiTheme.TextPrimary, Margin = new Padding(12, 10, 4, 0) });
            panelTop.Controls.Add(_dtFrom);
            panelTop.Controls.Add(new Label { Text = "По:", AutoSize = true, ForeColor = UiTheme.TextPrimary, Margin = new Padding(8, 10, 4, 0) });
            panelTop.Controls.Add(_dtTo);

            var row2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            row2.Controls.Add(_btnRefresh);
            row2.Controls.Add(_btnAdd);
            row2.Controls.Add(_btnEdit);
            row2.Controls.Add(_btnDelete);
            row2.Controls.Add(_btnPost);
            panelTop.Controls.Add(row2);

            MaterialStyle.StyleDataGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToOrderColumns = true;

            var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            host.Controls.Add(panelTop, 0, 0);
            host.Controls.Add(_grid, 0, 1);
            Controls.Add(host);

            _btnRefresh.Click += (_, __) => Reload();
            _btnAdd.Click += BtnAdd_Click;
            _btnEdit.Click += BtnEdit_Click;
            _btnDelete.Click += BtnDelete_Click;
            _btnPost.Click += BtnPost_Click;
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
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                    BtnEdit_Click(_, EventArgs.Empty);
            };
            DataGridViewSearchHighlighter.Attach(_grid, _txtSearch);
            Load += DocumentsUserControl_Load;
        }

        private void DocumentsUserControl_Load(object sender, EventArgs e)
        {
            var canEdit = RolePermissionService.HasPermission(ModuleKeys.DocumentsEdit);
            _btnAdd.Enabled = canEdit;
            _btnEdit.Enabled = canEdit;
            _btnDelete.Enabled = canEdit;
            _btnPost.Enabled = RolePermissionService.HasPermission(ModuleKeys.DocumentsPost);
            InitStatuses();
        }

        private void InitStatuses()
        {
            try
            {
                using (var db = new Lera_Diploma.Data.FinancialDbContext())
                {
                    var list = db.DocumentStatuses.OrderBy(x => x.Name).ToList();
                    _cbStatus.SelectedIndexChanged += (_, __) => Reload();
                    _cbStatus.Items.Add(new StatusItem(null, "Все"));
                    foreach (var st in list)
                        _cbStatus.Items.Add(new StatusItem(st.Id, st.Name));
                    _cbStatus.SelectedIndex = 0;
                }
                _dtFrom.Value = DateTime.Today.AddMonths(-6);
                _dtTo.Value = DateTime.Today.AddDays(1);
                Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class StatusItem
        {
            public int? Id { get; }
            public string Name { get; }
            public StatusItem(int? id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }

        private int? GetSelectedId()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
                return null;
            var v = _grid.CurrentRow.Cells["Id"].Value;
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
        }

        private void Reload()
        {
            var svc = new DocumentService();
            int? st = null;
            if (_cbStatus.SelectedItem is StatusItem si && si.Id.HasValue)
                st = si.Id.Value;
            var list = svc.GetDocuments(_txtSearch.Text, st, _dtFrom.Value.Date, _dtTo.Value.Date);
            var rows = list.Select(x => new
            {
                x.Id,
                x.Number,
                x.DocumentDate,
                Тип = x.DocumentType.Name,
                Статус = x.DocumentStatus.Name,
                Контрагент = x.Counterparty != null ? x.Counterparty.Name : "",
                Ответственный = x.ResponsibleUser.FullName
            });
            _grid.DataSource = EnumerableToDataTable.FromRows(rows);
            GridHeaderMap.Apply(_grid, "documents", "Id");
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var f = new DocumentEditForm(null))
            {
                if (f.ShowDialog(FindForm()) == DialogResult.OK && f.Saved)
                {
                    UserFeedback.Info(FindForm(), "Документ успешно сохранён.", "Документы");
                    Reload();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var id = GetSelectedId();
            if (!id.HasValue)
                return;
            using (var f = new DocumentEditForm(id.Value))
            {
                if (f.ShowDialog(FindForm()) == DialogResult.OK && f.Saved)
                {
                    UserFeedback.Info(FindForm(), "Документ успешно сохранён.", "Документы");
                    Reload();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var id = GetSelectedId();
            if (!id.HasValue)
                return;
            if (MessageBox.Show(FindForm(), "Удалить черновик документа?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var svc = new DocumentService();
            var err = svc.TryDelete(id.Value);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                UserFeedback.Deleted(FindForm(), "Черновик документа");
                Reload();
            }
        }

        private void BtnPost_Click(object sender, EventArgs e)
        {
            var id = GetSelectedId();
            if (!id.HasValue)
                return;
            var svc = new DocumentService();
            var msg = svc.TryPostDocument(id.Value, CurrentUserContext.UserId, out var err);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Проведение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                new AuditService().Write(CurrentUserContext.UserId, "PostDocument", "FinancialDocument", id.Value.ToString(), null);
                UserFeedback.Info(FindForm(), string.IsNullOrWhiteSpace(msg) ? "Документ успешно проведён." : msg, "Проведение");
                Reload();
            }
        }
    }
}

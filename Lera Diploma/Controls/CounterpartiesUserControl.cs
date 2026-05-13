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
    public class CounterpartiesUserControl : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnAdd = new Button { Text = "Добавить" };
        private readonly Button _btnEdit = new Button { Text = "Изменить" };
        private readonly Button _btnDelete = new Button { Text = "Удалить" };
        private readonly CounterpartyService _svc = new CounterpartyService();

        public CounterpartiesUserControl()
        {
            BackColor = UiTheme.PageBackground;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            var top = ModuleToolbar.CreateDockTopToolbar();
            MaterialStyle.StyleToolbarButton(_btnRefresh);
            MaterialStyle.StyleToolbarButton(_btnAdd, true);
            MaterialStyle.StyleToolbarButton(_btnEdit);
            MaterialStyle.StyleToolbarButton(_btnDelete);
            top.Controls.Add(_btnRefresh);
            top.Controls.Add(_btnAdd);
            top.Controls.Add(_btnEdit);
            top.Controls.Add(_btnDelete);
            MaterialStyle.StyleDataGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToOrderColumns = true;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && RolePermissionService.HasPermission(ModuleKeys.CounterpartiesEdit))
                {
                    var id = GetId();
                    if (id.HasValue)
                        EditRow(id.Value);
                }
            };

            var host = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            host.Controls.Add(top, 0, 0);
            host.Controls.Add(_grid, 0, 1);
            Controls.Add(host);
            _btnRefresh.Click += (_, __) => Reload();
            _btnAdd.Click += (_, __) => EditRow(null);
            _btnEdit.Click += (_, __) =>
            {
                var id = GetId();
                if (id.HasValue)
                    EditRow(id.Value);
            };
            _btnDelete.Click += (_, __) =>
            {
                var id = GetId();
                if (!id.HasValue)
                    return;
                if (MessageBox.Show(FindForm(), "Удалить контрагента?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                var err = _svc.TryDelete(id.Value);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    UserFeedback.Deleted(FindForm(), "Контрагент");
                    Reload();
                }
            };
            Load += CounterpartiesUserControl_Load;
        }

        private void CounterpartiesUserControl_Load(object sender, EventArgs e)
        {
            var can = RolePermissionService.HasPermission(ModuleKeys.CounterpartiesEdit);
            _btnAdd.Enabled = can;
            _btnEdit.Enabled = can;
            _btnDelete.Enabled = can;
            Reload();
        }

        private int? GetId()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
                return null;
            var v = _grid.CurrentRow.Cells["Id"].Value;
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
        }

        private void EditRow(int? id)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.CounterpartiesEdit))
                return;
            var existing = id.HasValue ? _svc.GetAll().FirstOrDefault(x => x.Id == id.Value) : null;
            if (id.HasValue && existing == null)
                return;

            using (var f = new MaterialModalForm(id.HasValue ? "Контрагент" : "Новый контрагент", UiTheme.Primary, 480, 300, "counterparties"))
            {
                var txtName = new TextBox { Left = 0, Top = 0, Width = 400, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, Text = existing?.Name ?? "" };
                var txtInn = new TextBox { Left = 0, Top = 36, Width = 400, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, Text = existing?.Inn ?? "" };
                var txtKpp = new TextBox { Left = 0, Top = 72, Width = 400, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, Text = existing?.Kpp ?? "" };
                var cbKind = new ComboBox { Left = 0, Top = 108, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Top };
                cbKind.Items.AddRange(new object[] { "ЮЛ", "ИП", "ФЛ" });
                cbKind.SelectedItem = existing?.Kind ?? "ЮЛ";
                MaterialStyle.StyleTextBox(txtName);
                MaterialStyle.StyleTextBox(txtInn);
                MaterialStyle.StyleTextBox(txtKpp);
                MaterialStyle.StyleCombo(cbKind);

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0) };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

                var pFields = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0) };
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                void Row(string title, Control c, ref int rowIdx)
                {
                    pFields.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, rowIdx);
                    c.Dock = DockStyle.Fill;
                    pFields.Controls.Add(c, 1, rowIdx);
                    rowIdx++;
                }
                var row = 0;
                Row("Наименование:", txtName, ref row);
                Row("ИНН:", txtInn, ref row);
                Row("КПП:", txtKpp, ref row);
                Row("Тип:", cbKind, ref row);

                var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
                var ok = MaterialModalForm.CreateDialogButton("Сохранить", DialogResult.OK, true);
                var cancel = MaterialModalForm.CreateDialogButton("Отмена", DialogResult.Cancel, false);
                footer.Controls.Add(ok);
                footer.Controls.Add(cancel);

                root.Controls.Add(pFields, 0, 0);
                root.Controls.Add(footer, 0, 1);
                f.Body.Controls.Add(root);
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                if (f.ShowDialog(FindForm()) != DialogResult.OK)
                    return;

                var err = _svc.TrySave(id, txtName.Text, txtInn.Text, txtKpp.Text, cbKind.SelectedItem?.ToString(), out _);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    if (id.HasValue)
                        UserFeedback.Saved(FindForm(), "Контрагент");
                    else
                        UserFeedback.Created(FindForm(), "Контрагент");
                    Reload();
                }
            }
        }

        private void Reload()
        {
            var rows = _svc.GetAll().Select(x => new { x.Id, x.Name, x.Inn, x.Kpp, x.Kind });
            _grid.DataSource = EnumerableToDataTable.FromRows(rows);
            GridHeaderMap.Apply(_grid, "counterparties", "Id");
        }
    }
}

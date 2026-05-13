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
    public class AccountsUserControl : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnAdd = new Button { Text = "Добавить" };
        private readonly Button _btnEdit = new Button { Text = "Изменить" };
        private readonly Button _btnDelete = new Button { Text = "Удалить" };
        private readonly AccountService _svc = new AccountService();

        public AccountsUserControl()
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
                if (e.RowIndex >= 0 && RolePermissionService.HasPermission(ModuleKeys.AccountsEdit))
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
                if (MessageBox.Show(FindForm(), "Удалить счёт?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                var err = _svc.TryDelete(id.Value);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    UserFeedback.Deleted(FindForm(), "Счёт в плане счетов");
                    Reload();
                }
            };
            Load += AccountsUserControl_Load;
        }

        private void AccountsUserControl_Load(object sender, EventArgs e)
        {
            var can = RolePermissionService.HasPermission(ModuleKeys.AccountsEdit);
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

        private sealed class IdName
        {
            public int Id { get; }
            public string Text { get; }
            public IdName(int id, string text) { Id = id; Text = text; }
            public override string ToString() => Text;
        }

        private void EditRow(int? id)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.AccountsEdit))
                return;
            var existing = id.HasValue ? _svc.GetAll().FirstOrDefault(x => x.Id == id.Value) : null;
            if (id.HasValue && existing == null)
                return;

            using (var f = new MaterialModalForm(id.HasValue ? "Счёт" : "Новый счёт", UiTheme.Warning, 520, 280, "accounts"))
            {
                var txtCode = new TextBox { Width = 340, Text = existing?.Code ?? "" };
                var txtName = new TextBox { Width = 340, Text = existing?.Name ?? "" };
                var parents = _svc.GetAll().Where(x => !id.HasValue || x.Id != id.Value).OrderBy(x => x.Code).ToList();
                var cbParent = new ComboBox { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
                cbParent.Items.Add("(нет)");
                cbParent.SelectedIndex = 0;
                foreach (var p in parents)
                {
                    cbParent.Items.Add(new IdName(p.Id, p.Code + " " + p.Name));
                    if (existing?.ParentAccountId == p.Id)
                        cbParent.SelectedIndex = cbParent.Items.Count - 1;
                }
                MaterialStyle.StyleTextBox(txtCode);
                MaterialStyle.StyleTextBox(txtName);
                MaterialStyle.StyleCombo(cbParent);

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

                var pFields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                int r = 0;
                void Row(string title, Control c)
                {
                    pFields.Controls.Add(new Label { Text = title, AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, r);
                    c.Dock = DockStyle.Fill;
                    pFields.Controls.Add(c, 1, r);
                    r++;
                }
                Row("Код:", txtCode);
                Row("Наименование:", txtName);
                Row("Родитель:", cbParent);

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

                int? parentId = null;
                if (cbParent.SelectedItem is IdName idn)
                    parentId = idn.Id;

                var err = _svc.TrySave(id, txtCode.Text, txtName.Text, parentId, out _);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    if (id.HasValue)
                        UserFeedback.Saved(FindForm(), "Счёт в плане счетов");
                    else
                        UserFeedback.Created(FindForm(), "Счёт в плане счетов");
                    Reload();
                }
            }
        }

        private void Reload()
        {
            var rows = _svc.GetAll().Select(x => new { x.Id, x.Code, x.Name, ParentId = x.ParentAccountId });
            _grid.DataSource = EnumerableToDataTable.FromRows(rows);
            GridHeaderMap.Apply(_grid, "accounts", "Id", "ParentId");
        }
    }
}

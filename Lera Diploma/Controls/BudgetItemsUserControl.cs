using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Lera_Diploma.Forms;
using Lera_Diploma.Models;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class BudgetItemsUserControl : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnAdd = new Button { Text = "Добавить" };
        private readonly Button _btnEdit = new Button { Text = "Изменить" };
        private readonly Button _btnDelete = new Button { Text = "Удалить" };
        private readonly BudgetItemService _svc = new BudgetItemService();

        public BudgetItemsUserControl()
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
                if (e.RowIndex >= 0 && RolePermissionService.HasPermission(ModuleKeys.BudgetEdit))
                {
                    var id = GetSelectedId();
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
                var id = GetSelectedId();
                if (id.HasValue)
                    EditRow(id.Value);
            };
            _btnDelete.Click += (_, __) =>
            {
                var id = GetSelectedId();
                if (!id.HasValue)
                    return;
                if (MessageBox.Show(FindForm(), "Удалить статью?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                var err = _svc.TryDelete(id.Value);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    UserFeedback.Deleted(FindForm(), "Статья бюджета");
                    Reload();
                }
            };
            Load += (_, __) => ApplyPerms();
            Load += (_, __) => Reload();
        }

        private void ApplyPerms()
        {
            var can = RolePermissionService.HasPermission(ModuleKeys.BudgetEdit);
            _btnAdd.Enabled = can;
            _btnEdit.Enabled = can;
            _btnDelete.Enabled = can;
        }

        private int? GetSelectedId()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
                return null;
            var v = _grid.CurrentRow.Cells["Id"].Value;
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
        }

        private void EditRow(int? id)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.BudgetEdit))
                return;
            BudgetItem existing = null;
            if (id.HasValue)
            {
                existing = _svc.GetAll().FirstOrDefault(x => x.Id == id.Value);
                if (existing == null)
                    return;
            }

            using (var f = new MaterialModalForm(id.HasValue ? "Статья бюджета" : "Новая статья", UiTheme.Info, 460, 240, "budget"))
            {
                var txtCode = new TextBox { Width = 300, Text = existing?.Code ?? "" };
                var txtName = new TextBox { Width = 300, Text = existing?.Name ?? "" };
                MaterialStyle.StyleTextBox(txtCode);
                MaterialStyle.StyleTextBox(txtName);
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));

                var pFields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
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
                var err = _svc.TrySave(id, txtCode.Text, txtName.Text, out _);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    if (id.HasValue)
                        UserFeedback.Saved(FindForm(), "Статья бюджета");
                    else
                        UserFeedback.Created(FindForm(), "Статья бюджета");
                    Reload();
                }
            }
        }

        private void Reload()
        {
            var rows = _svc.GetAll().Select(x => new { x.Id, x.Code, x.Name });
            _grid.DataSource = EnumerableToDataTable.FromRows(rows);
            GridHeaderMap.Apply(_grid, "budget", "Id");
        }
    }
}

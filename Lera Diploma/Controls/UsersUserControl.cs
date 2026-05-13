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
    public class UsersUserControl : UserControl
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _btnRefresh = new Button { Text = "Обновить" };
        private readonly Button _btnAdd = new Button { Text = "Добавить" };
        private readonly Button _btnEdit = new Button { Text = "Изменить" };
        private readonly Button _btnDelete = new Button { Text = "Удалить" };
        private readonly Button _btnResetPwd = new Button { Text = "Сброс пароля" };
        private readonly Button _btnToggleActive = new Button { Text = "Вкл./выкл." };
        private readonly Button _btnRolePerms = new Button { Text = "Права ролей" };
        private readonly UserAdminService _svc = new UserAdminService();
        private bool _suspendIsActiveEvents;

        public UsersUserControl()
        {
            BackColor = UiTheme.PageBackground;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);
            var top = ModuleToolbar.CreateDockTopToolbar();
            MaterialStyle.StyleToolbarButton(_btnRefresh);
            MaterialStyle.StyleToolbarButton(_btnAdd, true);
            MaterialStyle.StyleToolbarButton(_btnEdit);
            MaterialStyle.StyleToolbarButton(_btnDelete);
            MaterialStyle.StyleToolbarButton(_btnResetPwd);
            MaterialStyle.StyleToolbarButton(_btnToggleActive);
            MaterialStyle.StyleToolbarButton(_btnRolePerms);
            top.Controls.Add(_btnRefresh);
            top.Controls.Add(_btnAdd);
            top.Controls.Add(_btnEdit);
            top.Controls.Add(_btnDelete);
            top.Controls.Add(_btnResetPwd);
            top.Controls.Add(_btnToggleActive);
            top.Controls.Add(_btnRolePerms);
            MaterialStyle.StyleDataGrid(_grid);
            _grid.Dock = DockStyle.Fill;
            _grid.AllowUserToOrderColumns = true;
            _grid.AllowUserToAddRows = false;
            _grid.ReadOnly = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            _grid.CellValueChanged += Grid_CellValueChanged;

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
            _btnResetPwd.Click += BtnResetPwd_Click;
            _btnToggleActive.Click += BtnToggleActive_Click;
            _btnRolePerms.Click += BtnRolePerms_Click;
            Load += (_, __) => Reload();
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewCheckBoxCell)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suspendIsActiveEvents || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;
            if (!_grid.Columns.Contains("IsActive") || _grid.Columns[e.ColumnIndex].Name != "IsActive")
                return;
            if (!RolePermissionService.HasPermission(ModuleKeys.Users))
                return;

            var row = _grid.Rows[e.RowIndex];
            var idObj = row.Cells["Id"].Value;
            if (idObj == null || idObj == DBNull.Value)
                return;
            var id = Convert.ToInt32(idObj);
            var newActive = GetRowBool(row, "IsActive");
            var err = _svc.TrySetActive(id, newActive);
            if (err != null)
            {
                MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _suspendIsActiveEvents = true;
                try
                {
                    row.Cells["IsActive"].Value = !newActive;
                }
                finally
                {
                    _suspendIsActiveEvents = false;
                }
            }
            else
                UserFeedback.Info(FindForm(), "Статус «Активен» сохранён.", "Пользователи");
        }

        private static bool GetRowBool(DataGridViewRow row, string columnName)
        {
            if (row == null || row.DataGridView == null || !row.DataGridView.Columns.Contains(columnName))
                return false;
            var v = row.Cells[columnName].Value;
            if (v == null || v == DBNull.Value)
                return false;
            try
            {
                return Convert.ToBoolean(v);
            }
            catch
            {
                return false;
            }
        }

        private int? GetId()
        {
            if (_grid.CurrentRow == null || !_grid.Columns.Contains("Id"))
                return null;
            var v = _grid.CurrentRow.Cells["Id"].Value;
            return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
        }

        private void BtnRolePerms_Click(object sender, EventArgs e)
        {
            if (!CurrentUserContext.IsAdmin)
            {
                MessageBox.Show(FindForm(), "Доступно только администратору.", "Права ролей", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var f = new RolePermissionsForm())
                f.ShowDialog(FindForm());
            RolePermissionService.LoadCurrentRolePermissions();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var f = new MaterialModalForm("Новый пользователь", UiTheme.Primary, 480, 340, "users"))
            {
                var txtLogin = new TextBox { Width = 300 };
                var txtName = new TextBox { Width = 300 };
                var txtPwd = new TextBox { Width = 300, PasswordChar = '●' };
                var cbRole = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var r in _svc.GetRoles())
                    cbRole.Items.Add(new RoleItem(r.Id, r.Name));
                if (cbRole.Items.Count > 0)
                    cbRole.SelectedIndex = 0;
                MaterialStyle.StyleTextBox(txtLogin);
                MaterialStyle.StyleTextBox(txtName);
                MaterialStyle.StyleTextBox(txtPwd);
                MaterialStyle.StyleCombo(cbRole);
                var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                int row = 0;
                void L(string t, Control c)
                {
                    p.Controls.Add(new Label { Text = t, AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, row);
                    c.Dock = DockStyle.Fill;
                    p.Controls.Add(c, 1, row);
                    row++;
                }
                L("Логин:", txtLogin);
                L("ФИО:", txtName);
                L("Пароль:", txtPwd);
                L("Роль:", cbRole);

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
                var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
                var ok = MaterialModalForm.CreateDialogButton("Сохранить", DialogResult.OK, true);
                var cancel = MaterialModalForm.CreateDialogButton("Отмена", DialogResult.Cancel, false);
                footer.Controls.Add(ok);
                footer.Controls.Add(cancel);
                root.Controls.Add(p, 0, 0);
                root.Controls.Add(footer, 0, 1);
                f.Body.Controls.Add(root);
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(FindForm()) != DialogResult.OK)
                    return;
                if (!(cbRole.SelectedItem is RoleItem ri))
                    return;
                var err = _svc.TryCreateUser(txtLogin.Text, txtName.Text, txtPwd.Text, ri.Id);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    UserFeedback.Created(FindForm(), "Пользователь");
                    Reload();
                }
            }
        }

        private sealed class RoleItem
        {
            public int Id { get; }
            public string Name { get; }
            public RoleItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var id = GetId();
            if (!id.HasValue)
                return;
            var login = _grid.CurrentRow.Cells["Login"].Value?.ToString();
            var fullName = _grid.CurrentRow.Cells["FullName"].Value?.ToString();
            var roleId = _svc.GetPrimaryRoleId(id.Value);
            using (var f = new MaterialModalForm("Пользователь: " + login, UiTheme.Primary, 460, 260, "users"))
            {
                var txtName = new TextBox { Width = 300, Text = fullName ?? "" };
                var cbRole = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var r in _svc.GetRoles())
                    cbRole.Items.Add(new RoleItem(r.Id, r.Name));
                for (var i = 0; i < cbRole.Items.Count; i++)
                {
                    if (cbRole.Items[i] is RoleItem rolePick && roleId.HasValue && rolePick.Id == roleId.Value)
                    {
                        cbRole.SelectedIndex = i;
                        break;
                    }
                }
                if (cbRole.SelectedIndex < 0 && cbRole.Items.Count > 0)
                    cbRole.SelectedIndex = 0;
                MaterialStyle.StyleTextBox(txtName);
                MaterialStyle.StyleCombo(cbRole);
                var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                p.Controls.Add(new Label { Text = "ФИО:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, 0);
                txtName.Dock = DockStyle.Fill;
                p.Controls.Add(txtName, 1, 0);
                p.Controls.Add(new Label { Text = "Роль:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, 1);
                cbRole.Dock = DockStyle.Fill;
                p.Controls.Add(cbRole, 1, 1);

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
                var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
                var ok = MaterialModalForm.CreateDialogButton("Сохранить", DialogResult.OK, true);
                var cancel = MaterialModalForm.CreateDialogButton("Отмена", DialogResult.Cancel, false);
                footer.Controls.Add(ok);
                footer.Controls.Add(cancel);
                root.Controls.Add(p, 0, 0);
                root.Controls.Add(footer, 0, 1);
                f.Body.Controls.Add(root);
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(FindForm()) != DialogResult.OK)
                    return;
                if (!(cbRole.SelectedItem is RoleItem ri))
                    return;
                var err = _svc.TryUpdateUser(id.Value, txtName.Text, ri.Id);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                {
                    UserFeedback.Saved(FindForm(), "Данные пользователя");
                    Reload();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var id = GetId();
            if (!id.HasValue)
                return;
            if (MessageBox.Show(FindForm(), "Удалить пользователя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            var err = _svc.TryDeleteUser(id.Value);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                UserFeedback.Deleted(FindForm(), "Пользователь");
                Reload();
            }
        }

        private void BtnResetPwd_Click(object sender, EventArgs e)
        {
            var id = GetId();
            if (!id.HasValue)
                return;
            using (var f = new MaterialModalForm("Новый пароль", UiTheme.Danger, 380, 180, "users"))
            {
                var txt = new TextBox { Width = 280, PasswordChar = '●' };
                MaterialStyle.StyleTextBox(txt);
                var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 8, 0, 0), AutoSize = true };
                p.Controls.Add(new Label { Text = "Пароль:", AutoSize = true });
                txt.Width = 280;
                p.Controls.Add(txt);

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
                var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
                var ok = MaterialModalForm.CreateDialogButton("OK", DialogResult.OK, true);
                var cancel = MaterialModalForm.CreateDialogButton("Отмена", DialogResult.Cancel, false);
                footer.Controls.Add(ok);
                footer.Controls.Add(cancel);
                root.Controls.Add(p, 0, 0);
                root.Controls.Add(footer, 0, 1);
                f.Body.Controls.Add(root);
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                if (f.ShowDialog(FindForm()) != DialogResult.OK || string.IsNullOrWhiteSpace(txt.Text))
                    return;
                var err = _svc.TryResetPassword(id.Value, txt.Text);
                if (err != null)
                    MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    UserFeedback.Info(FindForm(), "Пароль успешно изменён.", "Пароль");
            }
        }

        private void BtnToggleActive_Click(object sender, EventArgs e)
        {
            var id = GetId();
            if (!id.HasValue)
                return;
            var active = GetRowBool(_grid.CurrentRow, "IsActive");
            var err = _svc.TrySetActive(id.Value, !active);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                UserFeedback.Info(FindForm(), "Статус активности пользователя обновлён.", "Пользователи");
                Reload();
            }
        }

        private void Reload()
        {
            _suspendIsActiveEvents = true;
            try
            {
                _btnRolePerms.Visible = CurrentUserContext.IsAdmin;
                var raw = _svc.GetUsersForGrid();
                _grid.DataSource = EnumerableToDataTable.FromRows((System.Collections.IEnumerable)raw);
                GridHeaderMap.Apply(_grid, "users", "Id");
                if (!_grid.Columns.Contains("IsActive"))
                    return;
                var canUsers = RolePermissionService.HasPermission(ModuleKeys.Users);
                var col = _grid.Columns["IsActive"];
                if (!(col is DataGridViewCheckBoxColumn))
                {
                    var ix = col.Index;
                    var dpn = string.IsNullOrEmpty(col.DataPropertyName) ? "IsActive" : col.DataPropertyName;
                    _grid.Columns.Remove(col);
                    var chk = new DataGridViewCheckBoxColumn
                    {
                        Name = "IsActive",
                        DataPropertyName = dpn,
                        HeaderText = "Активен",
                        ReadOnly = !canUsers,
                        ThreeState = false
                    };
                    if (ix >= 0 && ix <= _grid.Columns.Count)
                        _grid.Columns.Insert(ix, chk);
                    else
                        _grid.Columns.Add(chk);
                }
                else
                {
                    col.HeaderText = "Активен";
                    col.ReadOnly = !canUsers;
                }

                foreach (DataGridViewColumn c in _grid.Columns)
                {
                    if (string.Equals(c.Name, "IsActive", StringComparison.OrdinalIgnoreCase))
                        c.ReadOnly = !canUsers;
                    else
                        c.ReadOnly = true;
                }
            }
            finally
            {
                _suspendIsActiveEvents = false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Forms
{
    /// <summary>Настройка набора прав для выбранной роли (только пользователь с ролью Admin).</summary>
    public sealed class RolePermissionsForm : MaterialModalForm
    {
        private readonly RolePermissionAdminService _svc = new RolePermissionAdminService();
        private readonly ComboBox _cbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        private readonly CheckedListBox _list = new CheckedListBox { CheckOnClick = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
        private readonly Button _btnSave = new Button { Text = "Сохранить" };
        private readonly Button _btnClose = new Button { Text = "Закрыть" };

        public RolePermissionsForm()
            : base("Права ролей", UiTheme.Primary, 580, 560, "users")
        {
            MaterialStyle.StyleCombo(_cbRole);
            MaterialStyle.StyleToolbarButton(_btnSave, true);
            MaterialStyle.StyleOutlinedButton(_btnClose);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(0) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));

            var lbl = new Label { Text = "Роль:", AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
            var pRole = new TableLayoutPanel { ColumnCount = 1, Dock = DockStyle.Fill, AutoSize = true };
            pRole.Controls.Add(lbl, 0, 0);
            pRole.Controls.Add(_cbRole, 0, 1);

            var hint = new Label
            {
                Text = "Отметьте действия, доступные для выбранной роли. Изменения вступают в силу после следующего входа пользователя.",
                AutoSize = true,
                ForeColor = UiTheme.TextMuted,
                MaximumSize = new Size(520, 0),
                Margin = new Padding(0, 0, 0, 8)
            };

            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            footer.Controls.Add(_btnClose);
            footer.Controls.Add(_btnSave);

            root.Controls.Add(pRole, 0, 0);
            root.Controls.Add(hint, 0, 1);
            root.Controls.Add(_list, 0, 2);
            root.Controls.Add(footer, 0, 3);
            Body.Controls.Add(root);

            foreach (var (key, caption) in ModuleKeys.AllPermissions)
                _list.Items.Add(new PermItem(key, caption), false);

            _cbRole.SelectedIndexChanged += (_, __) => LoadKeysForSelectedRole();
            _btnSave.Click += BtnSave_Click;
            _btnClose.Click += (_, __) => Close();

            Shown += (_, __) => LoadRoles();
        }

        private void LoadRoles()
        {
            var err = _svc.TryGetRoles(out var roles);
            if (err != null)
            {
                MessageBox.Show(this, err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }
            _cbRole.Items.Clear();
            foreach (var r in roles)
                _cbRole.Items.Add(new RolePick(r.Id, r.Code, r.Name));
            if (_cbRole.Items.Count > 0)
                _cbRole.SelectedIndex = 0;
        }

        private void LoadKeysForSelectedRole()
        {
            if (_cbRole.SelectedItem is not RolePick rp)
                return;
            var err = _svc.TryGetKeys(rp.Id, out var keys);
            if (err != null)
            {
                MessageBox.Show(this, err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            for (var i = 0; i < _list.Items.Count; i++)
            {
                if (_list.Items[i] is PermItem pi)
                    _list.SetItemChecked(i, keys.Contains(pi.Key));
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_cbRole.SelectedItem is not RolePick rp)
                return;
            var selected = new List<string>();
            for (var i = 0; i < _list.Items.Count; i++)
            {
                if (_list.GetItemChecked(i) && _list.Items[i] is PermItem pi)
                    selected.Add(pi.Key);
            }
            var err = _svc.TrySave(rp.Id, selected);
            if (err != null)
            {
                MessageBox.Show(this, err, "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            UserFeedback.Saved(this, "Права роли");
            RolePermissionService.LoadCurrentRolePermissions();
        }

        private sealed class RolePick
        {
            public int Id { get; }
            public string Code { get; }
            public string Name { get; }
            public RolePick(int id, string code, string name) { Id = id; Code = code; Name = name; }
            public override string ToString() => Name + " (" + Code + ")";
        }

        private sealed class PermItem
        {
            public string Key { get; }
            public string Caption { get; }
            public PermItem(string key, string caption) { Key = key; Caption = caption; }
            public override string ToString() => Caption;
        }
    }
}

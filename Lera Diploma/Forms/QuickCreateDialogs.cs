using System;
using System.Windows.Forms;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Forms
{
    /// <summary>Минимальные диалоги «создать и вернуться» из формы документа (кнопки +).</summary>
    public static class QuickCreateDialogs
    {
        public static bool TryCreateDocumentType(IWin32Window owner)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.DataQuickCreate))
            {
                MessageBox.Show(owner, "Нет права на быстрое создание из списков (кнопка «+»).", "Права", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (!RolePermissionService.HasPermission(ModuleKeys.ReferencesEdit))
            {
                MessageBox.Show(owner, "Нет права на редактирование справочников (типы документов).", "Права", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (!TwoFieldDialog(owner, "Новый тип документа", null, null, out var code, out var name))
                return false;
            var svc = new ReferenceDataService();
            var err = svc.TryAddDocumentType(code, name);
            if (err != null)
            {
                MessageBox.Show(owner, err, "Тип документа", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            UserFeedback.Created(owner, "Тип документа");
            return true;
        }

        public static bool TryCreateCounterparty(IWin32Window owner)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.DataQuickCreate))
            {
                MessageBox.Show(owner, "Нет права на быстрое создание из списков (кнопка «+»).", "Права", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (!RolePermissionService.HasPermission(ModuleKeys.CounterpartiesEdit))
            {
                MessageBox.Show(owner, "Нет права на редактирование контрагентов.", "Права", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            using (var f = new MaterialModalForm("Новый контрагент", UiTheme.Primary, 520, 360, "counterparties"))
            {
                var txtName = new TextBox();
                var txtInn = new TextBox();
                var txtKpp = new TextBox();
                var cbKind = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cbKind.Items.AddRange(new object[] { "ЮЛ", "ИП", "ФЛ" });
                cbKind.SelectedIndex = 0;
                MaterialStyle.StyleTextBox(txtName);
                MaterialStyle.StyleTextBox(txtInn);
                MaterialStyle.StyleTextBox(txtKpp);
                MaterialStyle.StyleCombo(cbKind);

                var pFields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, AutoSize = true, Padding = new Padding(0) };
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                for (var i = 0; i < 4; i++)
                    pFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                void Row(int row, string title, Control c)
                {
                    pFields.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 10, 8, 0) }, 0, row);
                    c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    pFields.Controls.Add(c, 1, row);
                }
                Row(0, "Наименование:", txtName);
                Row(1, "ИНН:", txtInn);
                Row(2, "КПП:", txtKpp);
                Row(3, "Тип:", cbKind);

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
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
                if (f.ShowDialog(owner) != DialogResult.OK)
                    return false;
                var cpSvc = new CounterpartyService();
                var err = cpSvc.TrySave(null, txtName.Text, txtInn.Text, txtKpp.Text, cbKind.SelectedItem?.ToString(), out _);
                if (err != null)
                {
                    MessageBox.Show(owner, err, "Контрагент", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                UserFeedback.Created(owner, "Контрагент");
                return true;
            }
        }

        public static bool TryCreateAccount(IWin32Window owner)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.DataQuickCreate))
            {
                MessageBox.Show(owner, "Нет права на быстрое создание из списков (кнопка «+»).", "Права", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (!RolePermissionService.HasPermission(ModuleKeys.AccountsEdit))
            {
                MessageBox.Show(owner, "Нет права на редактирование плана счетов.", "Права", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            using (var f = new MaterialModalForm("Новый счёт", UiTheme.Warning, 520, 280, "accounts"))
            {
                var txtCode = new TextBox();
                var txtName = new TextBox();
                MaterialStyle.StyleTextBox(txtCode);
                MaterialStyle.StyleTextBox(txtName);
                var pFields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, AutoSize = true };
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                pFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                pFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                pFields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                pFields.Controls.Add(new Label { Text = "Код:", AutoSize = true, Margin = new Padding(0, 10, 8, 0) }, 0, 0);
                txtCode.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                pFields.Controls.Add(txtCode, 1, 0);
                pFields.Controls.Add(new Label { Text = "Наименование:", AutoSize = true, Margin = new Padding(0, 10, 8, 0) }, 0, 1);
                txtName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                pFields.Controls.Add(txtName, 1, 1);

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
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
                if (f.ShowDialog(owner) != DialogResult.OK)
                    return false;
                var accSvc = new AccountService();
                var err = accSvc.TrySave(null, txtCode.Text, txtName.Text, null, out _);
                if (err != null)
                {
                    MessageBox.Show(owner, err, "Счёт", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                UserFeedback.Created(owner, "Счёт");
                return true;
            }
        }

        private static bool TwoFieldDialog(IWin32Window owner, string title, string codeInit, string nameInit, out string code, out string name)
        {
            code = name = null;
            using (var f = new MaterialModalForm(title, UiTheme.Primary, 440, 220, "references"))
            {
                var txtCode = new TextBox { Text = codeInit ?? "" };
                var txtName = new TextBox { Text = nameInit ?? "" };
                MaterialStyle.StyleTextBox(txtCode);
                MaterialStyle.StyleTextBox(txtName);
                var p = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                p.Controls.Add(new Label { Text = "Код:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, 0);
                txtCode.Dock = DockStyle.Fill;
                p.Controls.Add(txtCode, 1, 0);
                p.Controls.Add(new Label { Text = "Наименование:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) }, 0, 1);
                txtName.Dock = DockStyle.Fill;
                p.Controls.Add(txtName, 1, 1);

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
                if (f.ShowDialog(owner) != DialogResult.OK)
                    return false;
                code = txtCode.Text.Trim();
                name = txtName.Text.Trim();
                return true;
            }
        }
    }
}

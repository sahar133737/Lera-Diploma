using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Lera_Diploma.Forms;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Controls
{
    public class ReferencesUserControl : UserControl
    {
        private readonly DataGridView _gridTypes = new DataGridView();
        private readonly DataGridView _gridStatuses = new DataGridView();
        private readonly Button _btnRefreshTypes = new Button { Text = "Обновить" };
        private readonly Button _btnAddType = new Button { Text = "Добавить" };
        private readonly Button _btnEditType = new Button { Text = "Изменить" };
        private readonly Button _btnDelType = new Button { Text = "Удалить" };
        private readonly Button _btnRefreshStatuses = new Button { Text = "Обновить" };
        private readonly Button _btnAddStatus = new Button { Text = "Добавить" };
        private readonly Button _btnEditStatus = new Button { Text = "Изменить" };
        private readonly Button _btnDelStatus = new Button { Text = "Удалить" };
        private readonly ReferenceDataService _svc = new ReferenceDataService();

        public ReferencesUserControl()
        {
            BackColor = UiTheme.PageBackground;
            Dock = DockStyle.Fill;
            Padding = new Padding(16);

            StyleToolbar(_btnRefreshTypes, _btnAddType, _btnEditType, _btnDelType);
            StyleToolbar(_btnRefreshStatuses, _btnAddStatus, _btnEditStatus, _btnDelStatus);

            var tip = new ToolTip { InitialDelay = 200, ReshowDelay = 100, AutoPopDelay = 8000 };
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 300 };

            void BuildPanel(Panel host, Label sectionTitle, FlowLayoutPanel toolRow, DataGridView grid, string tipText)
            {
                host.Dock = DockStyle.Fill;
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                sectionTitle.Dock = DockStyle.Fill;
                sectionTitle.Margin = new Padding(0, 0, 0, 4);
                sectionTitle.AutoSize = true;
                toolRow.Dock = DockStyle.Fill;
                grid.Dock = DockStyle.Fill;
                root.Controls.Add(sectionTitle, 0, 0);
                root.Controls.Add(toolRow, 0, 1);
                root.Controls.Add(grid, 0, 2);
                host.Controls.Add(root);
                tip.SetToolTip(host, tipText);
            }

            var p1 = new Panel();
            var lblTypes = new Label { Text = "Типы документов", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = UiTheme.TextPrimary };
            var t1 = ModuleToolbar.CreateDockTopToolbar();
            t1.Controls.Add(_btnRefreshTypes);
            t1.Controls.Add(_btnAddType);
            t1.Controls.Add(_btnEditType);
            t1.Controls.Add(_btnDelType);
            MaterialStyle.StyleDataGrid(_gridTypes);
            BuildPanel(p1, lblTypes, t1, _gridTypes, "Справочник типов документов: код и наименование для классификации первичных документов.");

            var p2 = new Panel();
            var lblStatuses = new Label { Text = "Статусы документов", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = UiTheme.TextPrimary };
            var t2 = ModuleToolbar.CreateDockTopToolbar();
            t2.Controls.Add(_btnRefreshStatuses);
            t2.Controls.Add(_btnAddStatus);
            t2.Controls.Add(_btnEditStatus);
            t2.Controls.Add(_btnDelStatus);
            MaterialStyle.StyleDataGrid(_gridStatuses);
            BuildPanel(p2, lblStatuses, t2, _gridStatuses, "Справочник статусов (черновик, проведён и т.д.), используемых в жизненном цикле документа.");

            split.Panel1.Controls.Add(p1);
            split.Panel2.Controls.Add(p2);
            tip.SetToolTip(split, "Верхняя часть — типы документов, нижняя — статусы. Разделитель можно перетаскивать.");
            Controls.Add(split);

            _btnRefreshTypes.Click += (_, __) => Reload();
            _btnRefreshStatuses.Click += (_, __) => Reload();
            _btnAddType.Click += (_, __) => AddOrEditType(null);
            _btnEditType.Click += (_, __) => AddOrEditType(GetId(_gridTypes));
            _btnDelType.Click += (_, __) => DeleteType(GetId(_gridTypes));
            _btnAddStatus.Click += (_, __) => AddOrEditStatus(null);
            _btnEditStatus.Click += (_, __) => AddOrEditStatus(GetId(_gridStatuses));
            _btnDelStatus.Click += (_, __) => DeleteStatus(GetId(_gridStatuses));
            Load += ReferencesUserControl_Load;
        }

        private static void StyleToolbar(params Button[] buttons)
        {
            for (var i = 0; i < buttons.Length; i++)
                MaterialStyle.StyleToolbarButton(buttons[i], i == 1);
        }

        private void ReferencesUserControl_Load(object sender, EventArgs e)
        {
            var can = RolePermissionService.HasPermission(ModuleKeys.ReferencesEdit);
            foreach (var b in new[] { _btnAddType, _btnEditType, _btnDelType, _btnAddStatus, _btnEditStatus, _btnDelStatus })
                b.Enabled = can;
            Reload();
        }

        private static int? GetId(DataGridView g)
        {
            if (g.CurrentRow == null || !g.Columns.Contains("Id"))
                return null;
            return Convert.ToInt32(g.CurrentRow.Cells["Id"].Value);
        }

        private void Reload()
        {
            _gridTypes.DataSource = _svc.GetDocumentTypes().Select(x => new { x.Id, x.Code, x.Name }).ToList();
            _gridStatuses.DataSource = _svc.GetDocumentStatuses().Select(x => new { x.Id, x.Code, x.Name }).ToList();
            GridHeaderMap.Apply(_gridTypes, "budget", "Id");
            GridHeaderMap.Apply(_gridStatuses, "budget", "Id");
        }

        private bool TwoFieldDialog(string title, string codeInit, string nameInit, out string code, out string name)
        {
            code = name = null;
            using (var f = new MaterialModalForm(title, UiTheme.Primary, 420, 200, "references"))
            {
                var txtCode = new TextBox { Width = 240, Text = codeInit ?? "" };
                var txtName = new TextBox { Width = 240, Text = nameInit ?? "" };
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
                if (f.ShowDialog(FindForm()) != DialogResult.OK)
                    return false;
                code = txtCode.Text.Trim();
                name = txtName.Text.Trim();
                return true;
            }
        }

        private void AddOrEditType(int? id)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.ReferencesEdit))
                return;
            string codeInit = null, nameInit = null;
            if (id.HasValue)
            {
                var row = _svc.GetDocumentTypes().FirstOrDefault(x => x.Id == id.Value);
                if (row == null)
                    return;
                codeInit = row.Code;
                nameInit = row.Name;
            }
            if (!TwoFieldDialog(id.HasValue ? "Тип документа" : "Новый тип документа", codeInit, nameInit, out var code, out var name))
                return;
            string err;
            if (id.HasValue)
                err = _svc.TryUpdateDocumentType(id.Value, code, name);
            else
                err = _svc.TryAddDocumentType(code, name);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                if (id.HasValue)
                    UserFeedback.Saved(FindForm(), "Тип документа");
                else
                    UserFeedback.Created(FindForm(), "Тип документа");
                Reload();
            }
        }

        private void DeleteType(int? id)
        {
            if (!id.HasValue || !RolePermissionService.HasPermission(ModuleKeys.ReferencesEdit))
                return;
            if (MessageBox.Show(FindForm(), "Удалить тип документа?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var err = _svc.TryDeleteDocumentType(id.Value);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                UserFeedback.Deleted(FindForm(), "Тип документа");
                Reload();
            }
        }

        private void AddOrEditStatus(int? id)
        {
            if (!RolePermissionService.HasPermission(ModuleKeys.ReferencesEdit))
                return;
            string codeInit = null, nameInit = null;
            if (id.HasValue)
            {
                var row = _svc.GetDocumentStatuses().FirstOrDefault(x => x.Id == id.Value);
                if (row == null)
                    return;
                codeInit = row.Code;
                nameInit = row.Name;
            }
            if (!TwoFieldDialog(id.HasValue ? "Статус документа" : "Новый статус", codeInit, nameInit, out var code, out var name))
                return;
            string err;
            if (id.HasValue)
                err = _svc.TryUpdateDocumentStatus(id.Value, code, name);
            else
                err = _svc.TryAddDocumentStatus(code, name);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                if (id.HasValue)
                    UserFeedback.Saved(FindForm(), "Статус документа");
                else
                    UserFeedback.Created(FindForm(), "Статус документа");
                Reload();
            }
        }

        private void DeleteStatus(int? id)
        {
            if (!id.HasValue || !RolePermissionService.HasPermission(ModuleKeys.ReferencesEdit))
                return;
            if (MessageBox.Show(FindForm(), "Удалить статус документа?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var err = _svc.TryDeleteDocumentStatus(id.Value);
            if (err != null)
                MessageBox.Show(FindForm(), err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                UserFeedback.Deleted(FindForm(), "Статус документа");
                Reload();
            }
        }
    }
}

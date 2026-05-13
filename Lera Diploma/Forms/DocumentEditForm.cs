using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Forms
{
    public sealed class DocumentEditForm : MaterialModalForm
    {
        private sealed class AccountPick
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }

        private sealed class UserItem
        {
            public int Id { get; }
            public string Caption { get; }
            public UserItem(int id, string caption) { Id = id; Caption = caption; }
            public override string ToString() => Caption;
        }

        private sealed class IdItem
        {
            public int? Id { get; }
            public string Caption { get; }
            public IdItem(int? id, string caption) { Id = id; Caption = caption; }
            public override string ToString() => Caption;
        }

        private readonly int? _documentId;
        private readonly TextBox _txtNumber = new TextBox();
        private readonly DateTimePicker _dtDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly ComboBox _cbType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _cbCounterparty = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _cbUser = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _txtComment = new TextBox { Dock = DockStyle.Fill };
        private readonly Label _lblStatus = new Label { AutoSize = true, ForeColor = UiTheme.TextMuted, Margin = new Padding(0, 6, 0, 0) };
        private readonly DataGridView _gridEntries = new DataGridView();
        private readonly Button _btnAddRow = new Button { Text = "Добавить строку" };
        private readonly Button _btnDelRow = new Button { Text = "Удалить строку" };
        private readonly Button _btnOk = new Button { Text = "Сохранить" };
        private readonly Button _btnPost = new Button { Text = "Провести" };
        private readonly Button _btnCancel = new Button { Text = "Отмена" };
        private readonly Button _btnPlusType = new Button { Text = "+", Width = 32, Height = 28 };
        private readonly Button _btnPlusCounterparty = new Button { Text = "+", Width = 32, Height = 28 };
        private readonly Button _btnPlusAccount = new Button { Text = "+ счёт" };
        private bool _readOnly;
        private List<Account> _accounts = new List<Account>();
        private List<AccountPick> _accountPicks = new List<AccountPick>();

        public bool Saved { get; private set; }

        public DocumentEditForm(int? documentId)
            : base(documentId.HasValue ? "Документ" : "Новый документ", UiTheme.Primary, 840, 720, "documents")
        {
            _documentId = documentId;
            Build();
        }

        private void Build()
        {
            MaterialStyle.StyleToolbarButton(_btnOk, true);
            MaterialStyle.StyleToolbarButton(_btnPost, true);
            MaterialStyle.StyleOutlinedButton(_btnCancel);
            _btnPost.Enabled = false;
            MaterialStyle.StyleOutlinedButton(_btnAddRow);
            MaterialStyle.StyleOutlinedButton(_btnDelRow);
            MaterialStyle.StyleOutlinedButton(_btnPlusAccount);
            _btnPlusType.Font = new Font(_btnPlusType.Font.FontFamily, 12f, FontStyle.Bold);
            _btnPlusCounterparty.Font = new Font(_btnPlusCounterparty.Font.FontFamily, 12f, FontStyle.Bold);
            _btnPlusType.Margin = new Padding(4, 6, 0, 0);
            _btnPlusCounterparty.Margin = new Padding(4, 6, 0, 0);
            _btnPlusAccount.Margin = new Padding(8, 0, 0, 0);
            MaterialStyle.StyleOutlinedButton(_btnPlusType);
            MaterialStyle.StyleOutlinedButton(_btnPlusCounterparty);
            MaterialStyle.StyleTextBox(_txtNumber);
            MaterialStyle.StyleTextBox(_txtComment);
            MaterialStyle.StyleCombo(_cbType);
            MaterialStyle.StyleCombo(_cbCounterparty);
            MaterialStyle.StyleCombo(_cbUser);
            MaterialStyle.StyleDataGrid(_gridEntries);
            _gridEntries.AllowUserToAddRows = false;
            _gridEntries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            // Один клик по ячейке сразу открывает редактор (иначе FullRowSelect требует второго клика для ComboBox).
            _gridEntries.EditMode = DataGridViewEditMode.EditOnEnter;
            _gridEntries.DefaultCellStyle.Padding = Padding.Empty;
            _gridEntries.RowTemplate.Height = 28;
            _gridEntries.EditingControlShowing += GridEntries_EditingControlShowing;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            int r = 0;
            void AddField(string title, Control c)
            {
                fields.Controls.Add(new Label { Text = title, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 10, 8, 0) }, 0, r);
                c.Margin = new Padding(0, 6, 0, 0);
                fields.Controls.Add(c, 1, r);
                r++;
            }

            static Control WrapComboWithPlus(ComboBox cb, Button plus)
            {
                var host = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
                host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                cb.Dock = DockStyle.Fill;
                plus.Dock = DockStyle.Fill;
                plus.Margin = new Padding(4, 0, 0, 0);
                host.Controls.Add(cb, 0, 0);
                host.Controls.Add(plus, 1, 0);
                return host;
            }

            AddField("Номер", _txtNumber);
            AddField("Дата", _dtDate);
            AddField("Тип", WrapComboWithPlus(_cbType, _btnPlusType));
            AddField("Контрагент", WrapComboWithPlus(_cbCounterparty, _btnPlusCounterparty));
            _cbUser.Dock = DockStyle.Fill;
            AddField("Ответственный", _cbUser);
            AddField("Комментарий", _txtComment);
            fields.Controls.Add(_lblStatus, 1, r);

            var entriesPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            var entriesToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight };
            entriesToolbar.Controls.Add(_btnAddRow);
            entriesToolbar.Controls.Add(_btnDelRow);
            entriesToolbar.Controls.Add(_btnPlusAccount);
            _gridEntries.Dock = DockStyle.Fill;
            entriesPanel.Controls.Add(_gridEntries);
            entriesPanel.Controls.Add(entriesToolbar);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
            footer.Controls.Add(_btnOk);
            footer.Controls.Add(_btnPost);
            footer.Controls.Add(_btnCancel);

            root.Controls.Add(fields, 0, 0);
            root.Controls.Add(entriesPanel, 0, 1);
            root.Controls.Add(footer, 0, 2);

            Body.Controls.Add(root);

            _btnPlusType.Click += BtnPlusType_Click;
            _btnPlusCounterparty.Click += BtnPlusCounterparty_Click;
            _btnPlusAccount.Click += BtnPlusAccount_Click;
            _cbType.SelectedIndexChanged += (_, __) => RefreshPostButtonState();
            _cbCounterparty.SelectedIndexChanged += (_, __) => RefreshPostButtonState();
            _cbUser.SelectedIndexChanged += (_, __) => RefreshPostButtonState();
            _txtNumber.TextChanged += (_, __) => RefreshPostButtonState();
            _dtDate.ValueChanged += (_, __) => RefreshPostButtonState();
            _gridEntries.CellValueChanged += (_, __) => RefreshPostButtonState();
            _gridEntries.CellEndEdit += (_, __) => RefreshPostButtonState();

            _btnCancel.Click += (_, __) => Close();
            _btnOk.Click += BtnOk_Click;
            _btnPost.Click += BtnPost_Click;
            _btnAddRow.Click += (_, __) => AddEntryRow();
            _btnDelRow.Click += (_, __) => RemoveEntryRow();
            Shown += DocumentEditForm_Shown;
        }

        private void GridEntries_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (sender is not DataGridView grid)
                return;
            // FlatStyle.Flat у ComboBox в ячейке даёт смещение кнопки раскрытия; системная отрисовка выглядит ровно.
            if (e.Control is not ComboBox cb)
            {
                if (e.Control is TextBox tb)
                {
                    tb.Margin = Padding.Empty;
                    tb.BorderStyle = BorderStyle.Fixed3D;
                }
                return;
            }
            cb.FlatStyle = FlatStyle.Standard;
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Margin = Padding.Empty;
            cb.Padding = Padding.Empty;
            // Высота ComboBox по умолчанию меньше высоты строки — кнопка списка визуально «короче» строки; выравниваем по ячейке.
            void AlignComboHeight()
            {
                if (cb.IsDisposed || !cb.IsHandleCreated)
                    return;
                var cell = grid.CurrentCell;
                if (cell == null)
                    return;
                var r = grid.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, true);
                if (r.Height <= 0)
                    return;
                var h = Math.Max(22, r.Height - 1);
                try
                {
                    cb.Height = h;
                }
                catch
                {
                    // ignore
                }
            }
            AlignComboHeight();
            grid.BeginInvoke(new Action(AlignComboHeight));
            void IdleOnce(object _, EventArgs __)
            {
                Application.Idle -= IdleOnce;
                AlignComboHeight();
            }
            Application.Idle += IdleOnce;
        }

        private void DocumentEditForm_Shown(object sender, EventArgs e)
        {
            try
            {
                LoadLookups();
                InitEntryColumns();
                if (_documentId.HasValue)
                    LoadDocument(_documentId.Value);
                else
                {
                    SuggestNewNumber();
                    _dtDate.Value = DateTime.Today;
                    SelectUser(CurrentUserContext.UserId);
                    AddEntryRow();
                }

                ApplyReadOnly();
                ApplyQuickCreateVisibility();
                RefreshPostButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyReadOnly()
        {
            _readOnly = _lblStatus.Text.Contains("Проведён");
            if (!_readOnly)
                return;
            _txtNumber.ReadOnly = true;
            _dtDate.Enabled = false;
            _cbType.Enabled = false;
            _cbCounterparty.Enabled = false;
            _cbUser.Enabled = false;
            _txtComment.ReadOnly = true;
            _gridEntries.ReadOnly = true;
            _btnAddRow.Enabled = false;
            _btnDelRow.Enabled = false;
            _btnOk.Enabled = false;
            _btnPost.Enabled = false;
            _btnPlusType.Enabled = false;
            _btnPlusCounterparty.Enabled = false;
            _btnPlusAccount.Enabled = false;
        }

        private void SuggestNewNumber()
        {
            using (var db = new FinancialDbContext())
            {
                var y = DateTime.Today.Year;
                var max = db.FinancialDocuments.Where(x => x.Number.StartsWith("ДБР-" + y)).Select(x => x.Number).ToList()
                    .Select(x =>
                    {
                        var p = x.LastIndexOf('/');
                        if (p < 0 || p >= x.Length - 1)
                            return 0;
                        return int.TryParse(x.Substring(p + 1), out var n) ? n : 0;
                    }).DefaultIfEmpty(0).Max();
                _txtNumber.Text = $"ДБР-{y}/{(max + 1):0000}";
            }
        }

        private void LoadLookups()
        {
            using (var db = new FinancialDbContext())
            {
                _accounts = db.Accounts.OrderBy(x => x.Code).ToList();
                _accountPicks = _accounts.Select(a => new AccountPick { Id = a.Id, Text = a.Code + " — " + a.Name }).ToList();
                _cbType.Items.Clear();
                _cbCounterparty.Items.Clear();
                _cbUser.Items.Clear();
                foreach (var t in db.DocumentTypes.OrderBy(x => x.Name))
                    _cbType.Items.Add(new IdItem(t.Id, t.Name));
                _cbCounterparty.Items.Add(new IdItem(null, "(не указан)"));
                foreach (var c in db.Counterparties.OrderBy(x => x.Name))
                    _cbCounterparty.Items.Add(new IdItem(c.Id, c.Name));
                foreach (var u in db.Users.Where(x => x.IsActive).OrderBy(x => x.FullName))
                    _cbUser.Items.Add(new UserItem(u.Id, u.FullName + " (" + u.Login + ")"));
            }
            if (_cbType.Items.Count > 0)
                _cbType.SelectedIndex = 0;
            if (_cbCounterparty.Items.Count > 0)
                _cbCounterparty.SelectedIndex = 0;
            if (_cbUser.Items.Count > 0)
                _cbUser.SelectedIndex = 0;
        }

        private void InitEntryColumns()
        {
            _gridEntries.Columns.Clear();
            // Важно: CellTemplate до DataSource/DisplayMember/ValueMember. Иначе источник данных
            // остаётся на стандартной ячейке конструктора, а после замены шаблона на FullHeightComboBoxCell
            // список счетов в ячейках пустой → «Недопустимое значение DataGridViewComboBoxCell».
            var colDebit = new DataGridViewComboBoxColumn
            {
                CellTemplate = new FullHeightComboBoxCell(),
                Name = "DebitAccountId",
                HeaderText = "Дебет",
                DataPropertyName = "DebitAccountId",
                DataSource = new List<AccountPick>(_accountPicks),
                DisplayMember = "Text",
                ValueMember = "Id",
                Width = 235,
                MinimumWidth = 170,
                FlatStyle = FlatStyle.Standard
            };
            var colCredit = new DataGridViewComboBoxColumn
            {
                CellTemplate = new FullHeightComboBoxCell(),
                Name = "CreditAccountId",
                HeaderText = "Кредит",
                DataPropertyName = "CreditAccountId",
                DataSource = new List<AccountPick>(_accountPicks),
                DisplayMember = "Text",
                ValueMember = "Id",
                Width = 235,
                MinimumWidth = 170,
                FlatStyle = FlatStyle.Standard
            };
            colDebit.DefaultCellStyle.Padding = Padding.Empty;
            colCredit.DefaultCellStyle.Padding = Padding.Empty;
            _gridEntries.Columns.Add(colDebit);
            _gridEntries.Columns.Add(colCredit);
            _gridEntries.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Сумма", Width = 115, MinimumWidth = 90 });
            _gridEntries.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Purpose",
                HeaderText = "Назначение",
                MinimumWidth = 220,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
        }

        private void LoadDocument(int id)
        {
            var svc = new DocumentService();
            var doc = svc.GetById(id);
            if (doc == null)
            {
                MessageBox.Show(this, "Документ не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }
            _txtNumber.Text = doc.Number;
            _dtDate.Value = doc.DocumentDate;
            SelectCombo(_cbType, doc.DocumentTypeId);
            if (doc.CounterpartyId.HasValue)
                SelectCombo(_cbCounterparty, doc.CounterpartyId.Value);
            else
                _cbCounterparty.SelectedIndex = 0;
            SelectUser(doc.ResponsibleUserId);
            _txtComment.Text = doc.Comment;
            _lblStatus.Text = "Статус: " + doc.DocumentStatus.Name;

            _gridEntries.Rows.Clear();
            foreach (var line in doc.Entries.OrderBy(x => x.LineNo))
            {
                var i = _gridEntries.Rows.Add();
                _gridEntries.Rows[i].Cells["DebitAccountId"].Value = line.DebitAccountId;
                _gridEntries.Rows[i].Cells["CreditAccountId"].Value = line.CreditAccountId;
                _gridEntries.Rows[i].Cells["Amount"].Value = line.Amount.ToString("0.00");
                _gridEntries.Rows[i].Cells["Purpose"].Value = line.Purpose;
            }
            if (_gridEntries.Rows.Count == 0)
                AddEntryRow();
            RefreshPostButtonState();
        }

        private static void SelectCombo(ComboBox cb, int id)
        {
            for (var i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is IdItem ii && ii.Id.HasValue && ii.Id.Value == id)
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SelectUser(int id)
        {
            for (var i = 0; i < _cbUser.Items.Count; i++)
            {
                if (_cbUser.Items[i] is UserItem u && u.Id == id)
                {
                    _cbUser.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ApplyQuickCreateVisibility()
        {
            var q = RolePermissionService.HasPermission(ModuleKeys.DataQuickCreate);
            _btnPlusType.Visible = q && RolePermissionService.HasPermission(ModuleKeys.ReferencesEdit);
            _btnPlusCounterparty.Visible = q && RolePermissionService.HasPermission(ModuleKeys.CounterpartiesEdit);
            _btnPlusAccount.Visible = q && RolePermissionService.HasPermission(ModuleKeys.AccountsEdit);
        }

        private void RefreshPostButtonState()
        {
            if (_readOnly || !RolePermissionService.HasPermission(ModuleKeys.DocumentsPost))
            {
                _btnPost.Enabled = false;
                return;
            }
            _btnPost.Enabled = !TryGetPostBlockingReason(out _);
        }

        private bool TryGetPostBlockingReason(out string message)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(_txtNumber.Text))
            {
                message = "Укажите номер документа.";
                return true;
            }
            if (!(_cbType.SelectedItem is IdItem ti) || !ti.Id.HasValue || ti.Id.Value <= 0)
            {
                message = "Выберите тип документа.";
                return true;
            }
            if (!(_cbUser.SelectedItem is UserItem u0) || u0.Id <= 0)
            {
                message = "Выберите ответственного.";
                return true;
            }
            var hasLine = false;
            foreach (DataGridViewRow row in _gridEntries.Rows)
            {
                if (row.IsNewRow)
                    continue;
                var debitVal = row.Cells["DebitAccountId"].Value;
                var creditVal = row.Cells["CreditAccountId"].Value;
                if (debitVal == null || creditVal == null)
                    continue;
                if (!TryParseAccountId(debitVal, out var debitId) || !TryParseAccountId(creditVal, out var creditId))
                    continue;
                hasLine = true;
                if (debitId == creditId)
                {
                    message = "В строке проводки дебет и кредит не должны совпадать.";
                    return true;
                }
                if (!TryParseMoney(row.Cells["Amount"].Value?.ToString(), out var amount) || amount <= 0)
                {
                    message = "Укажите положительные суммы во всех строках проводок.";
                    return true;
                }
                var purpose = row.Cells["Purpose"].Value?.ToString()?.Trim();
                if (string.IsNullOrEmpty(purpose))
                {
                    message = "Заполните назначение платежа во всех строках проводок (колонка «Назначение»).";
                    return true;
                }
            }
            if (!hasLine)
            {
                message = "Добавьте хотя бы одну проводку с выбранными счетами.";
                return true;
            }
            return false;
        }

        private void BtnPlusType_Click(object sender, EventArgs e)
        {
            if (!QuickCreateDialogs.TryCreateDocumentType(this))
                return;
            ReloadDocumentTypesCombo();
            RefreshPostButtonState();
        }

        private void BtnPlusCounterparty_Click(object sender, EventArgs e)
        {
            if (!QuickCreateDialogs.TryCreateCounterparty(this))
                return;
            ReloadCounterpartiesCombo();
            RefreshPostButtonState();
        }

        private void BtnPlusAccount_Click(object sender, EventArgs e)
        {
            if (!QuickCreateDialogs.TryCreateAccount(this))
                return;
            ReloadAccountsOnly();
            RefreshPostButtonState();
        }

        private void ReloadDocumentTypesCombo()
        {
            var id = (_cbType.SelectedItem as IdItem)?.Id;
            _cbType.Items.Clear();
            using (var db = new FinancialDbContext())
            {
                foreach (var t in db.DocumentTypes.OrderBy(x => x.Name))
                    _cbType.Items.Add(new IdItem(t.Id, t.Name));
            }
            if (id.HasValue)
                SelectCombo(_cbType, id.Value);
            else if (_cbType.Items.Count > 0)
                _cbType.SelectedIndex = 0;
        }

        private void ReloadCounterpartiesCombo()
        {
            var id = (_cbCounterparty.SelectedItem as IdItem)?.Id;
            _cbCounterparty.Items.Clear();
            using (var db = new FinancialDbContext())
            {
                _cbCounterparty.Items.Add(new IdItem(null, "(не указан)"));
                foreach (var c in db.Counterparties.OrderBy(x => x.Name))
                    _cbCounterparty.Items.Add(new IdItem(c.Id, c.Name));
            }
            if (id.HasValue)
                SelectCombo(_cbCounterparty, id.Value);
            else
                _cbCounterparty.SelectedIndex = 0;
        }

        private void ReloadAccountsOnly()
        {
            using (var db = new FinancialDbContext())
            {
                _accounts = db.Accounts.OrderBy(x => x.Code).ToList();
                _accountPicks = _accounts.Select(a => new AccountPick { Id = a.Id, Text = a.Code + " — " + a.Name }).ToList();
            }
            RefreshAccountComboColumnsOnly();
        }

        private void RefreshAccountComboColumnsOnly()
        {
            if (!_gridEntries.Columns.Contains("DebitAccountId"))
                return;
            var list = new List<AccountPick>(_accountPicks);
            if (_gridEntries.Columns["DebitAccountId"] is DataGridViewComboBoxColumn d)
                d.DataSource = list;
            if (_gridEntries.Columns["CreditAccountId"] is DataGridViewComboBoxColumn c)
                c.DataSource = new List<AccountPick>(_accountPicks);
        }

        private void AddEntryRow()
        {
            if (_readOnly)
                return;
            var debit = _accounts.FirstOrDefault(x => x.Code.StartsWith("401")) ?? _accounts.First();
            var credit = _accounts.FirstOrDefault(x => x.Code == "1010" || x.Code == "2010") ?? _accounts.Skip(1).FirstOrDefault() ?? _accounts.First();
            var i = _gridEntries.Rows.Add();
            _gridEntries.Rows[i].Cells["DebitAccountId"].Value = debit.Id;
            _gridEntries.Rows[i].Cells["CreditAccountId"].Value = credit.Id;
            _gridEntries.Rows[i].Cells["Amount"].Value = "0.00";
            _gridEntries.Rows[i].Cells["Purpose"].Value = "";
            RefreshPostButtonState();
        }

        private void RemoveEntryRow()
        {
            if (_readOnly || _gridEntries.CurrentRow == null)
                return;
            _gridEntries.Rows.Remove(_gridEntries.CurrentRow);
            RefreshPostButtonState();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_readOnly)
                return;
            if (!TryPersistDraft(out _))
                return;
            Saved = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnPost_Click(object sender, EventArgs e)
        {
            if (_readOnly || !RolePermissionService.HasPermission(ModuleKeys.DocumentsPost))
                return;
            if (TryGetPostBlockingReason(out var block))
            {
                MessageBox.Show(this, block, "Проведение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!TryPersistDraft(out var savedId))
                return;
            var svc = new DocumentService();
            var msg = svc.TryPostDocument(savedId, CurrentUserContext.UserId, out var postErr);
            if (postErr != null)
            {
                MessageBox.Show(this, postErr, "Проведение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            UserFeedback.Info(this, string.IsNullOrWhiteSpace(msg) ? "Документ проведён." : msg, "Проведение");
            Saved = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>Сохраняет черновик (новый или существующий) и возвращает Id в БД.</summary>
        private bool TryPersistDraft(out int savedId)
        {
            savedId = 0;
            if (_readOnly)
                return false;
            var model = BuildSaveModel();
            if (model == null)
                return false;
            var svc = new DocumentService();
            var err = svc.TrySave(model, out savedId);
            if (err != null)
            {
                MessageBox.Show(this, err, "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private DocumentSaveModel BuildSaveModel()
        {
            var typeId = (_cbType.SelectedItem as IdItem)?.Id ?? 0;
            var model = new DocumentSaveModel
            {
                Id = _documentId,
                Number = _txtNumber.Text,
                DocumentDate = _dtDate.Value,
                DocumentTypeId = typeId,
                CounterpartyId = (_cbCounterparty.SelectedItem as IdItem)?.Id,
                ResponsibleUserId = (_cbUser.SelectedItem as UserItem)?.Id ?? 0,
                Comment = _txtComment.Text
            };
            foreach (DataGridViewRow row in _gridEntries.Rows)
            {
                if (row.IsNewRow)
                    continue;
                var debitVal = row.Cells["DebitAccountId"].Value;
                var creditVal = row.Cells["CreditAccountId"].Value;
                if (debitVal == null || creditVal == null)
                    continue;
                if (!TryParseAccountId(debitVal, out var debitId) || !TryParseAccountId(creditVal, out var creditId))
                {
                    MessageBox.Show(this, "Проверьте выбор счетов в строках проводок.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                if (!TryParseMoney(row.Cells["Amount"].Value?.ToString(), out var amount))
                {
                    MessageBox.Show(this, "Проверьте суммы в строках проводок.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                model.Entries.Add(new DocumentEntrySaveRow
                {
                    DebitAccountId = debitId,
                    CreditAccountId = creditId,
                    Amount = amount,
                    LineNo = model.Entries.Count + 1,
                    Purpose = row.Cells["Purpose"].Value?.ToString()
                });
            }
            return model;
        }

        private static bool TryParseAccountId(object val, out int id)
        {
            id = 0;
            if (val == null || val == DBNull.Value)
                return false;
            switch (val)
            {
                case int i:
                    id = i;
                    return true;
                case short s:
                    id = s;
                    return true;
                case long l when l >= int.MinValue && l <= int.MaxValue:
                    id = (int)l;
                    return true;
            }
            return int.TryParse(val.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
        }

        private static bool TryParseMoney(string s, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(s))
                return false;
            var t = s.Trim().Replace(" ", "").Replace("\u00A0", "");
            if (decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                return true;
            return decimal.TryParse(t, NumberStyles.Number, CultureInfo.GetCultureInfo("ru-RU"), out amount);
        }
    }
}

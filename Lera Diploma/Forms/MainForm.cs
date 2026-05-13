using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Lera_Diploma.Controls;
using Lera_Diploma.Infrastructure;
using Lera_Diploma.Security;
using Lera_Diploma.Services;
using Lera_Diploma.UI;

namespace Lera_Diploma.Forms
{
    public partial class MainForm : Form
    {
        public bool LogoutRequested { get; private set; }

        private Panel _menuScrollHost;
        private FlowLayoutPanel _flowMenu;
        private Button _btnLogout;
        private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private string _currentModuleKey = "default";

        public MainForm()
        {
            InitializeComponent();
            BuildSidebar();
            WireModuleTitleBar();
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
            Text = "ИС финансового отдела — Дубровский район";
            Navigate(new DashboardUserControl(), "dashboard", "Главное меню");
            ApplyPermissions();
        }

        private void BuildSidebar()
        {
            panelSidebar.Controls.Clear();
            _navButtons.Clear();

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(12, 8, 12, 12), BackColor = UiTheme.SidebarBg };
            _btnLogout = CreateNavButton("Выйти", false);
            _btnLogout.Dock = DockStyle.Fill;
            _btnLogout.Click += BtnLogout_Click;
            bottom.Controls.Add(_btnLogout);

            _menuScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UiTheme.SidebarBg,
                Padding = Padding.Empty
            };
            _flowMenu = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(12, 16, 12, 8),
                BackColor = UiTheme.SidebarBg
            };
            _menuScrollHost.Controls.Add(_flowMenu);
            _menuScrollHost.Resize += (_, __) => SyncSidebarMenuWidth();
            Load += (_, __) => BeginInvoke(new Action(SyncSidebarMenuWidth));

            var logo = new Label
            {
                Text = "ФО\nДубровский район",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = UiTheme.Primary,
                AutoSize = true,
                Margin = new Padding(4, 0, 0, 12)
            };
            _flowMenu.Controls.Add(logo);

            AddNav("dashboard", "Главное меню", BtnDashboard_Click);
            AddNav("documents", "Документы", BtnDocuments_Click);
            AddNav("ledger", "Журнал проводок", BtnLedger_Click);
            AddNav("counterparties", "Контрагенты", BtnCounterparties_Click);
            AddNav("budget", "Статьи бюджета", BtnBudget_Click);
            AddNav("references", "Справочники", BtnReferences_Click);
            AddNav("accounts", "План счетов", BtnAccounts_Click);
            AddNav("reports", "Отчёты", BtnReports_Click);
            AddNav("users", "Пользователи", BtnUsers_Click);
            AddNav("audit", "Журнал аудита", BtnAudit_Click);
            AddNav("backup", "Резервное копирование", BtnBackup_Click);

            panelSidebar.Controls.Add(_menuScrollHost);
            panelSidebar.Controls.Add(bottom);
            SyncSidebarMenuWidth();
        }

        private void SyncSidebarMenuWidth()
        {
            if (_menuScrollHost == null || _flowMenu == null)
                return;
            var w = _menuScrollHost.ClientSize.Width;
            if (_menuScrollHost.VerticalScroll.Visible)
                w -= SystemInformation.VerticalScrollBarWidth;
            w = Math.Max(96, w);
            _flowMenu.SuspendLayout();
            try
            {
                _flowMenu.Width = w;
                var pad = _flowMenu.Padding.Left + _flowMenu.Padding.Right;
                foreach (Control c in _flowMenu.Controls)
                {
                    if (c is Button b && b.Tag is string)
                    {
                        var inner = w - pad - b.Margin.Horizontal;
                        var textW = TextRenderer.MeasureText(b.Text, b.Font, Size.Empty, TextFormatFlags.SingleLine).Width + 28;
                        b.Width = Math.Max(80, Math.Max(inner, textW));
                    }
                }
            }
            finally
            {
                _flowMenu.ResumeLayout(true);
            }

            _menuScrollHost.HorizontalScroll.Enabled = false;
            _menuScrollHost.HorizontalScroll.Visible = false;
        }

        private void AddNav(string key, string text, EventHandler onClick)
        {
            var b = CreateNavButton(text, false);
            b.Width = 200;
            b.Height = 40;
            b.Margin = new Padding(0, 0, 0, 8);
            b.Tag = key;
            b.Click += (s, e) =>
            {
                onClick(s, e);
                SetActiveNav(key);
            };
            _flowMenu.Controls.Add(b);
            _navButtons[key] = b;
        }

        private static Button CreateNavButton(string text, bool active)
        {
            var b = new Button
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            ApplyNavVisual(b, active);
            return b;
        }

        private static void ApplyNavVisual(Button b, bool active)
        {
            if (active)
            {
                b.BackColor = UiTheme.Primary;
                b.ForeColor = Color.White;
                b.FlatAppearance.MouseOverBackColor = UiTheme.PrimaryDark;
            }
            else
            {
                b.BackColor = UiTheme.SidebarBg;
                b.ForeColor = UiTheme.TextPrimary;
                b.FlatAppearance.MouseOverBackColor = Color.FromArgb(243, 229, 245);
            }
        }

        private void SetActiveNav(string moduleKey)
        {
            foreach (var kv in _navButtons)
                ApplyNavVisual(kv.Value, string.Equals(kv.Key, moduleKey, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyPermissions()
        {
            SetNavEnabled("dashboard", RolePermissionService.HasPermission(ModuleKeys.Dashboard));
            SetNavEnabled("documents", RolePermissionService.HasPermission(ModuleKeys.Documents));
            SetNavEnabled("ledger", RolePermissionService.HasPermission(ModuleKeys.Ledger));
            SetNavEnabled("counterparties", RolePermissionService.HasPermission(ModuleKeys.Counterparties));
            SetNavEnabled("budget", RolePermissionService.HasPermission(ModuleKeys.Budget));
            SetNavEnabled("references", RolePermissionService.HasPermission(ModuleKeys.References));
            SetNavEnabled("accounts", RolePermissionService.HasPermission(ModuleKeys.Accounts));
            SetNavEnabled("reports", RolePermissionService.HasPermission(ModuleKeys.Reports));
            SetNavEnabled("users", RolePermissionService.HasPermission(ModuleKeys.Users));
            SetNavEnabled("audit", RolePermissionService.HasPermission(ModuleKeys.Audit));
            SetNavEnabled("backup", RolePermissionService.HasPermission(ModuleKeys.Backup));
        }

        private void SetNavEnabled(string key, bool enabled)
        {
            if (_navButtons.TryGetValue(key, out var b))
                b.Enabled = enabled;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.F11)
                return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ModuleHelpProvider.ShowHelp(_currentModuleKey, this);
        }

        private void Navigate(UserControl control, string moduleKey, string title)
        {
            _currentModuleKey = moduleKey;
            lblModuleTitle.Text = title + "   |   " + CurrentUserContext.FullName + " (" + CurrentUserContext.RoleDisplayName + ")";
            panelHost.Controls.Clear();
            control.Dock = DockStyle.Fill;
            panelHost.Controls.Add(control);
            SetActiveNav(moduleKey);
        }

        private void WireModuleTitleBar()
        {
            panelTop.Controls.Clear();
            panelTop.MinimumSize = new Size(0, 52);
            var wrap = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 2, 0, 2) };
            wrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            wrap.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblModuleTitle.Dock = DockStyle.Fill;
            lblModuleTitle.AutoSize = false;
            lblModuleTitle.AutoEllipsis = true;
            lblModuleTitle.TextAlign = ContentAlignment.MiddleLeft;
            var btnHelp = new Button { Text = "Справка", AutoSize = true, Margin = new Padding(12, 2, 0, 2) };
            MaterialStyle.StyleOutlinedButton(btnHelp);
            btnHelp.Click += (_, __) => ModuleHelpProvider.ShowHelp(_currentModuleKey, this);
            wrap.Controls.Add(lblModuleTitle, 0, 0);
            wrap.Controls.Add(btnHelp, 1, 0);
            panelTop.Controls.Add(wrap);
        }

        private void BtnDashboard_Click(object sender, EventArgs e) =>
            Navigate(new DashboardUserControl(), "dashboard", "Главное меню");

        private void BtnDocuments_Click(object sender, EventArgs e) =>
            Navigate(new DocumentsUserControl(), "documents", "Документы и проводки");

        private void BtnLedger_Click(object sender, EventArgs e) =>
            Navigate(new LedgerUserControl(), "ledger", "Журнал проводок");

        private void BtnCounterparties_Click(object sender, EventArgs e) =>
            Navigate(new CounterpartiesUserControl(), "counterparties", "Контрагенты");

        private void BtnBudget_Click(object sender, EventArgs e) =>
            Navigate(new BudgetItemsUserControl(), "budget", "Статьи бюджета");

        private void BtnReferences_Click(object sender, EventArgs e) =>
            Navigate(new ReferencesUserControl(), "references", "Справочники");

        private void BtnAccounts_Click(object sender, EventArgs e) =>
            Navigate(new AccountsUserControl(), "accounts", "План счетов");

        private void BtnReports_Click(object sender, EventArgs e) =>
            Navigate(new ReportsUserControl(), "reports", "Отчёты");

        private void BtnUsers_Click(object sender, EventArgs e) =>
            Navigate(new UsersUserControl(), "users", "Пользователи");

        private void BtnAudit_Click(object sender, EventArgs e) =>
            Navigate(new AuditLogUserControl(), "audit", "Журнал аудита");

        private void BtnBackup_Click(object sender, EventArgs e) =>
            Navigate(new BackupUserControl(), "backup", "Резервное копирование");

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            LogoutRequested = true;
            new AuditService().Write(CurrentUserContext.UserId, "Logout", "User", CurrentUserContext.Login, "Выход из системы");
            CurrentUserContext.Clear();
            RolePermissionService.Clear();
            Close();
        }
    }
}

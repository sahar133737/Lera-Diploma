using System.Collections.Generic;

namespace Lera_Diploma.Services
{
    /// <summary>Ключи прав доступа к модулям (стиль BGSK1: module.*).</summary>
    public static class ModuleKeys
    {
        public const string Dashboard = "module.dashboard";
        public const string Documents = "module.documents";
        public const string DocumentsEdit = "module.documents.edit";
        public const string DocumentsPost = "module.documents.post";
        public const string Ledger = "module.ledger";
        public const string Counterparties = "module.counterparties";
        public const string CounterpartiesEdit = "module.counterparties.edit";
        public const string Budget = "module.budget";
        public const string BudgetEdit = "module.budget.edit";
        public const string References = "module.references";
        public const string ReferencesEdit = "module.references.edit";
        public const string Accounts = "module.accounts";
        public const string AccountsEdit = "module.accounts.edit";
        public const string Reports = "module.reports";
        public const string ReportsExport = "module.reports.export";
        public const string Users = "module.users";
        /// <summary>Настройка набора прав по ролям (только супер-админ через роль Admin).</summary>
        public const string RolesPermissions = "module.roles.permissions";
        /// <summary>Кнопки «+» для быстрого создания элементов справочников из форм документов.</summary>
        public const string DataQuickCreate = "module.data.quickcreate";
        public const string Audit = "module.audit";
        public const string Backup = "module.backup";

        /// <summary>Все известные ключи с подписями для UI настройки ролей.</summary>
        public static IReadOnlyList<(string Key, string Caption)> AllPermissions { get; } = new List<(string Key, string Caption)>
        {
            (Dashboard, "Главное меню"),
            (Documents, "Документы (просмотр списка)"),
            (DocumentsEdit, "Документы: создание и редактирование"),
            (DocumentsPost, "Документы: проведение"),
            (Ledger, "Журнал проводок"),
            (Counterparties, "Контрагенты (просмотр)"),
            (CounterpartiesEdit, "Контрагенты: редактирование"),
            (Budget, "Статьи бюджета (просмотр)"),
            (BudgetEdit, "Статьи бюджета: редактирование"),
            (References, "Справочники (просмотр)"),
            (ReferencesEdit, "Справочники: редактирование"),
            (Accounts, "План счетов (просмотр)"),
            (AccountsEdit, "План счетов: редактирование"),
            (Reports, "Отчёты"),
            (ReportsExport, "Отчёты: экспорт / печать"),
            (Users, "Пользователи"),
            (RolesPermissions, "Настройка прав ролей"),
            (DataQuickCreate, "Быстрое создание из списков (+)"),
            (Audit, "Журнал аудита"),
            (Backup, "Резервное копирование")
        };
    }
}

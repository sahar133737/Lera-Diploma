using System;
using System.Collections.Generic;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;

namespace Lera_Diploma.Services
{
    /// <summary>Набор прав по умолчанию для кода роли и первичное заполнение таблицы RolePermissions.</summary>
    public static class RolePermissionDefaults
    {
        public static readonly IReadOnlyDictionary<string, string[]> RoleCodeToKeys =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Admin"] = new[]
                {
                    ModuleKeys.Dashboard, ModuleKeys.Documents, ModuleKeys.DocumentsEdit, ModuleKeys.DocumentsPost, ModuleKeys.Ledger,
                    ModuleKeys.Counterparties, ModuleKeys.CounterpartiesEdit, ModuleKeys.Budget, ModuleKeys.BudgetEdit,
                    ModuleKeys.References, ModuleKeys.ReferencesEdit, ModuleKeys.Accounts, ModuleKeys.AccountsEdit,
                    ModuleKeys.Reports, ModuleKeys.ReportsExport, ModuleKeys.Users, ModuleKeys.RolesPermissions,
                    ModuleKeys.DataQuickCreate, ModuleKeys.Audit, ModuleKeys.Backup
                },
                ["ChiefAccountant"] = new[]
                {
                    ModuleKeys.Dashboard, ModuleKeys.Documents, ModuleKeys.DocumentsEdit, ModuleKeys.DocumentsPost, ModuleKeys.Ledger,
                    ModuleKeys.Counterparties, ModuleKeys.CounterpartiesEdit, ModuleKeys.Budget, ModuleKeys.BudgetEdit,
                    ModuleKeys.References, ModuleKeys.ReferencesEdit, ModuleKeys.Accounts,
                    ModuleKeys.Reports, ModuleKeys.ReportsExport, ModuleKeys.DataQuickCreate, ModuleKeys.Audit, ModuleKeys.Backup
                },
                ["Accountant"] = new[]
                {
                    ModuleKeys.Dashboard, ModuleKeys.Documents, ModuleKeys.DocumentsEdit, ModuleKeys.Ledger,
                    ModuleKeys.Counterparties, ModuleKeys.CounterpartiesEdit, ModuleKeys.Budget,
                    ModuleKeys.References, ModuleKeys.Accounts,
                    ModuleKeys.Reports, ModuleKeys.ReportsExport
                },
                ["Viewer"] = new[]
                {
                    ModuleKeys.Dashboard, ModuleKeys.Documents, ModuleKeys.Ledger,
                    ModuleKeys.Counterparties, ModuleKeys.Budget, ModuleKeys.References, ModuleKeys.Accounts,
                    ModuleKeys.Reports
                }
            };

        /// <summary>Создаёт таблицу RolePermissions на существующей БД без пересоздания (EF6 не мигрирует схему).</summary>
        public static void EnsureTable(FinancialDbContext db)
        {
            if (db == null)
                return;
            const string sql = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = SCHEMA_NAME() AND TABLE_NAME = 'RolePermissions')
BEGIN
    CREATE TABLE [dbo].[RolePermissions](
        [RoleId] [int] NOT NULL,
        [PermissionKey] [nvarchar](128) NOT NULL,
        CONSTRAINT [PK_dbo.RolePermissions] PRIMARY KEY CLUSTERED ([RoleId] ASC, [PermissionKey] ASC)
    );
    ALTER TABLE [dbo].[RolePermissions] WITH CHECK ADD CONSTRAINT [FK_dbo.RolePermissions_dbo.Roles_RoleId]
        FOREIGN KEY([RoleId]) REFERENCES [dbo].[Roles] ([Id]) ON DELETE CASCADE;
END";
            db.Database.ExecuteSqlCommand(sql);
        }

        /// <summary>Добавляет отсутствующие строки прав для всех ролей (идемпотентно).</summary>
        public static void SeedMissing(FinancialDbContext db)
        {
            if (db == null || !db.Roles.Any())
                return;

            var roles = db.Roles.ToList();
            foreach (var r in roles)
            {
                if (!RoleCodeToKeys.TryGetValue(r.Code, out var keys))
                    continue;
                foreach (var k in keys)
                {
                    if (string.IsNullOrWhiteSpace(k))
                        continue;
                    if (db.RolePermissions.Any(x => x.RoleId == r.Id && x.PermissionKey == k))
                        continue;
                    db.RolePermissions.Add(new RolePermission { RoleId = r.Id, PermissionKey = k });
                }
            }

            db.SaveChanges();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    /// <summary>Загрузка набора разрешений после входа (по ролям из БД, как RolePermissionService в BGSK1).</summary>
    public static class RolePermissionService
    {
        private static readonly HashSet<string> Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string[]> RoleToModules =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Admin"] = new[]
                {
                    ModuleKeys.Dashboard, ModuleKeys.Documents, ModuleKeys.DocumentsEdit, ModuleKeys.DocumentsPost, ModuleKeys.Ledger,
                    ModuleKeys.Counterparties, ModuleKeys.CounterpartiesEdit, ModuleKeys.Budget, ModuleKeys.BudgetEdit,
                    ModuleKeys.References, ModuleKeys.ReferencesEdit, ModuleKeys.Accounts, ModuleKeys.AccountsEdit,
                    ModuleKeys.Reports, ModuleKeys.ReportsExport, ModuleKeys.Users, ModuleKeys.Audit, ModuleKeys.Backup
                },
                ["ChiefAccountant"] = new[]
                {
                    ModuleKeys.Dashboard, ModuleKeys.Documents, ModuleKeys.DocumentsEdit, ModuleKeys.DocumentsPost, ModuleKeys.Ledger,
                    ModuleKeys.Counterparties, ModuleKeys.CounterpartiesEdit, ModuleKeys.Budget, ModuleKeys.BudgetEdit,
                    ModuleKeys.References, ModuleKeys.ReferencesEdit, ModuleKeys.Accounts,
                    ModuleKeys.Reports, ModuleKeys.ReportsExport, ModuleKeys.Audit, ModuleKeys.Backup
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

        public static void LoadCurrentRolePermissions()
        {
            Permissions.Clear();
            if (CurrentUserContext.RoleCodes == null)
                return;
            foreach (var role in CurrentUserContext.RoleCodes)
            {
                if (RoleToModules.TryGetValue(role, out var mods))
                {
                    foreach (var m in mods)
                        Permissions.Add(m);
                }
            }
        }

        public static bool HasPermission(string permissionKey) =>
            !string.IsNullOrWhiteSpace(permissionKey) && Permissions.Contains(permissionKey);

        public static void Clear() => Permissions.Clear();
    }
}

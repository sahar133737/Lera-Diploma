using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    /// <summary>Эффективные разрешения текущего пользователя (по таблице RolePermissions в БД).</summary>
    public static class RolePermissionService
    {
        private static readonly HashSet<string> Permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void LoadCurrentRolePermissions()
        {
            Permissions.Clear();
            if (!CurrentUserContext.IsAuthenticated)
                return;

            try
            {
                using (var db = new FinancialDbContext())
                {
                    var roleIds = db.UserRoles.AsNoTracking()
                        .Where(x => x.UserId == CurrentUserContext.UserId)
                        .Select(x => x.RoleId)
                        .ToList();
                    if (roleIds.Count == 0)
                        return;

                    var keys = db.RolePermissions.AsNoTracking()
                        .Where(x => roleIds.Contains(x.RoleId))
                        .Select(x => x.PermissionKey)
                        .Distinct()
                        .ToList();
                    foreach (var k in keys)
                    {
                        if (!string.IsNullOrWhiteSpace(k))
                            Permissions.Add(k.Trim());
                    }
                }
            }
            catch
            {
                // При несовпадении схемы БД не блокируем вход — пустой набор прав.
            }
        }

        public static bool HasPermission(string permissionKey) =>
            !string.IsNullOrWhiteSpace(permissionKey) && Permissions.Contains(permissionKey.Trim());

        public static void Clear() => Permissions.Clear();
    }
}

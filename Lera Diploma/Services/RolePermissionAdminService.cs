using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    /// <summary>Редактирование прав ролей (только администратор с ролью Admin).</summary>
    public sealed class RolePermissionAdminService
    {
        public string TryGetRoles(out List<(int Id, string Code, string Name)> roles)
        {
            roles = new List<(int, string, string)>();
            if (!CurrentUserContext.IsAdmin)
                return "Доступно только администратору.";
            using (var db = new FinancialDbContext())
            {
                foreach (var r in db.Roles.AsNoTracking().OrderBy(x => x.Name))
                    roles.Add((r.Id, r.Code, r.Name));
            }
            return null;
        }

        public string TryGetKeys(int roleId, out HashSet<string> keys)
        {
            keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!CurrentUserContext.IsAdmin)
                return "Доступно только администратору.";
            using (var db = new FinancialDbContext())
            {
                if (!db.Roles.Any(x => x.Id == roleId))
                    return "Роль не найдена.";
                foreach (var k in db.RolePermissions.AsNoTracking().Where(x => x.RoleId == roleId).Select(x => x.PermissionKey))
                    keys.Add(k);
            }
            return null;
        }

        public string TrySave(int roleId, IReadOnlyCollection<string> selectedKeys)
        {
            if (!CurrentUserContext.IsAdmin)
                return "Доступно только администратору.";

            var valid = new HashSet<string>(ModuleKeys.AllPermissions.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var incoming = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (selectedKeys != null)
            {
                foreach (var k in selectedKeys)
                {
                    if (!string.IsNullOrWhiteSpace(k) && valid.Contains(k.Trim()))
                        incoming.Add(k.Trim());
                }
            }

            using (var db = new FinancialDbContext())
            {
                var role = db.Roles.FirstOrDefault(x => x.Id == roleId);
                if (role == null)
                    return "Роль не найдена.";

                if (string.Equals(role.Code, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    if (!incoming.Contains(ModuleKeys.Users) || !incoming.Contains(ModuleKeys.RolesPermissions))
                        return "У роли «Администратор» должны остаться права «Пользователи» и «Настройка прав ролей».";
                }

                var existing = db.RolePermissions.Where(x => x.RoleId == roleId).ToList();
                foreach (var e in existing)
                    db.RolePermissions.Remove(e);

                foreach (var k in incoming)
                    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionKey = k });

                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "UpdateRolePermissions", "Role", role.Code, string.Join(",", incoming.OrderBy(x => x)));
                return null;
            }
        }
    }
}

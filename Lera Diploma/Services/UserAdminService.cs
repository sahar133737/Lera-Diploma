using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    public sealed class UserAdminService
    {
        public object GetUsersForGrid()
        {
            using (var db = new FinancialDbContext())
            {
                return db.Users.Include("UserRoles.Role").OrderBy(x => x.Login).ToList().Select(u => new
                {
                    u.Id,
                    u.Login,
                    u.FullName,
                    u.IsActive,
                    Роли = string.Join(", ", u.UserRoles.Select(ur => ur.Role.Name))
                }).ToList();
            }
        }

        public string TryResetPassword(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                return "Пароль не короче 4 символов.";
            using (var db = new FinancialDbContext())
            {
                var u = db.Users.Find(userId);
                if (u == null)
                    return "Пользователь не найден.";
                u.PasswordHash = PasswordHasher.HashPassword(newPassword);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "ResetPassword", "User", u.Login, null);
                return null;
            }
        }

        public string TrySetActive(int userId, bool isActive)
        {
            using (var db = new FinancialDbContext())
            {
                var u = db.Users.Find(userId);
                if (u == null)
                    return "Не найдено.";
                if (u.Id == CurrentUserContext.UserId && !isActive)
                    return "Нельзя деактивировать самого себя.";
                u.IsActive = isActive;
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "SetActive", "User", u.Login, isActive.ToString());
                return null;
            }
        }

        public string TryCreateUser(string login, string fullName, string password, int roleId)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(fullName))
                return "Заполните логин и ФИО.";
            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
                return "Пароль не короче 4 символов.";
            using (var db = new FinancialDbContext())
            {
                var loginTrim = login.Trim();
                if (db.Users.Any(x => x.Login == loginTrim))
                    return "Пользователь с таким логином уже есть.";
                var roleRow = db.Roles.FirstOrDefault(x => x.Id == roleId);
                if (roleRow == null)
                    return "Роль не найдена.";
                if (string.Equals(roleRow.Code, "Admin", StringComparison.OrdinalIgnoreCase) && !CurrentUserContext.IsAdmin)
                    return "Назначать роль администратора может только администратор.";
                var u = new User
                {
                    Login = loginTrim,
                    FullName = fullName.Trim(),
                    PasswordHash = PasswordHasher.HashPassword(password),
                    IsActive = true
                };
                db.Users.Add(u);
                db.SaveChanges();
                db.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = roleId });
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Create", "User", u.Login, null);
                return null;
            }
        }

        public string TryUpdateUser(int userId, string fullName, int roleId)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return "Укажите ФИО.";
            using (var db = new FinancialDbContext())
            {
                var u = db.Users.Include("UserRoles").FirstOrDefault(x => x.Id == userId);
                if (u == null)
                    return "Не найдено.";
                var roleRow = db.Roles.FirstOrDefault(x => x.Id == roleId);
                if (roleRow == null)
                    return "Роль не найдена.";
                if (string.Equals(roleRow.Code, "Admin", StringComparison.OrdinalIgnoreCase) && !CurrentUserContext.IsAdmin)
                    return "Назначать роль администратора может только администратор.";
                u.FullName = fullName.Trim();
                var existing = u.UserRoles.ToList();
                foreach (var ur in existing)
                    db.UserRoles.Remove(ur);
                db.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = roleId });
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Update", "User", u.Login, null);
                return null;
            }
        }

        public string TryDeleteUser(int userId)
        {
            using (var db = new FinancialDbContext())
            {
                if (userId == CurrentUserContext.UserId)
                    return "Нельзя удалить текущего пользователя.";
                var u = db.Users.Include("UserRoles").FirstOrDefault(x => x.Id == userId);
                if (u == null)
                    return "Не найдено.";
                if (db.FinancialDocuments.Any(x => x.ResponsibleUserId == userId))
                    return "Нельзя удалить: пользователь указан ответственным в документах.";
                foreach (var ur in u.UserRoles.ToList())
                    db.UserRoles.Remove(ur);
                db.Users.Remove(u);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Delete", "User", userId.ToString(), u.Login);
                return null;
            }
        }

        public List<Role> GetRoles()
        {
            using (var db = new FinancialDbContext())
                return db.Roles.OrderBy(x => x.Name).ToList();
        }

        public int? GetPrimaryRoleId(int userId)
        {
            using (var db = new FinancialDbContext())
            {
                var ur = db.UserRoles.FirstOrDefault(x => x.UserId == userId);
                return ur?.RoleId;
            }
        }
    }
}

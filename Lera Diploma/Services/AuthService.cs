using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    public sealed class AuthService
    {
        private static readonly Dictionary<string, int> FailedAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private const int MaxAttempts = 5;

        public bool TryLogin(string login, string password, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Введите логин и пароль.";
                return false;
            }

            login = login.Trim();
            if (FailedAttempts.TryGetValue(login, out var n) && n >= MaxAttempts)
            {
                errorMessage = "Учётная запись временно заблокирована из-за неудачных попыток входа.";
                return false;
            }

            try
            {
                using (var db = new FinancialDbContext())
                {
                    var user = db.Users.Include("UserRoles.Role")
                        .FirstOrDefault(x => x.Login == login && x.IsActive);

                    if (user == null || !PasswordHasher.Verify(password, user.PasswordHash))
                    {
                        FailedAttempts[login] = FailedAttempts.TryGetValue(login, out var c) ? c + 1 : 1;
                        errorMessage = "Неверный логин или пароль.";
                        return false;
                    }

                    FailedAttempts.Remove(login);
                    var roles = user.UserRoles.Select(ur => ur.Role.Code).Distinct().ToList();
                    CurrentUserContext.Set(user.Id, user.Login, user.FullName, roles);
                    RolePermissionService.LoadCurrentRolePermissions();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Ошибка подключения к базе данных: " + ex.Message;
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lera_Diploma.Security
{
    /// <summary>Текущий пользователь сессии (аналог BGSK1.CurrentUserContext).</summary>
    public static class CurrentUserContext
    {
        public static int UserId { get; private set; }
        public static string Login { get; private set; }
        public static string FullName { get; private set; }
        public static IReadOnlyList<string> RoleCodes { get; private set; } = Array.Empty<string>();

        /// <summary>Первая роль для отображения в заголовке.</summary>
        public static string RoleDisplayName =>
            RoleCodes == null || RoleCodes.Count == 0 ? "" : string.Join(", ", RoleCodes);

        public static bool IsAuthenticated => UserId > 0;

        public static void Set(int userId, string login, string fullName, IEnumerable<string> roleCodes)
        {
            UserId = userId;
            Login = login ?? "";
            FullName = fullName ?? "";
            RoleCodes = roleCodes?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                        ?? (IReadOnlyList<string>)Array.Empty<string>();
        }

        public static void Clear()
        {
            UserId = 0;
            Login = "";
            FullName = "";
            RoleCodes = Array.Empty<string>();
        }
    }
}

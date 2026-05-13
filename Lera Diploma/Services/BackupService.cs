using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace Lera_Diploma.Services
{
    public sealed class BackupService
    {
        public string TryBackupToFile(string fullPath, out string error)
        {
            error = null;
            try
            {
                var cs = Infrastructure.Db.AppConnectionString;
                if (string.IsNullOrWhiteSpace(cs))
                {
                    error = "Строка подключения FinanceContext не найдена.";
                    return null;
                }

                var dbName = ExtractInitialCatalog(cs);
                if (string.IsNullOrEmpty(dbName))
                {
                    error = "Не удалось определить имя базы из строки подключения.";
                    return null;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");

                var bakSql = $"BACKUP DATABASE [{dbName.Replace("]", "]]")}] TO DISK = @p WITH FORMAT, INIT, NAME = N'Finance backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
                using (var conn = new SqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(bakSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", fullPath);
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }
                }

                return "Резервная копия создана.";
            }
            catch (Exception ex)
            {
                error = FormatBackupError(ex);
                return null;
            }
        }

        public string TryRestoreFromFile(string fullPath, out string error)
        {
            error = null;
            try
            {
                var cs = Infrastructure.Db.AppConnectionString;
                if (string.IsNullOrWhiteSpace(cs))
                {
                    error = "Строка подключения не найдена.";
                    return null;
                }

                var dbName = ExtractInitialCatalog(cs);
                if (string.IsNullOrEmpty(dbName))
                {
                    error = "Не удалось определить имя базы.";
                    return null;
                }

                using (var conn = new SqlConnection(cs))
                {
                    conn.Open();
                    var single = $"ALTER DATABASE [{dbName.Replace("]", "]]")}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                    using (var cmd = new SqlCommand(single, conn)) cmd.ExecuteNonQuery();

                    var restore = $"RESTORE DATABASE [{dbName.Replace("]", "]]")}] FROM DISK = @p WITH REPLACE, RECOVERY;";
                    using (var cmd = new SqlCommand(restore, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", fullPath);
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }

                    var multi = $"ALTER DATABASE [{dbName.Replace("]", "]]")}] SET MULTI_USER;";
                    using (var cmd = new SqlCommand(multi, conn)) cmd.ExecuteNonQuery();
                }

                return "Восстановление выполнено. Перезапустите приложение.";
            }
            catch (Exception ex)
            {
                error = FormatBackupError(ex);
                return null;
            }
        }

        private static string FormatBackupError(Exception ex)
        {
            var msg = ex?.Message ?? "";
            var hint = "";
            if (msg.IndexOf("Operating system error", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("Cannot open backup device", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("доступ к пути", StringComparison.OrdinalIgnoreCase) >= 0
                || msg.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hint = "\n\nСлужба SQL Server пишет файл под своей учётной записью (часто NT SERVICE\\MSSQL$…), а не под вашим пользователем Windows." +
                       "\nУкажите папку, где у этой службы есть права на запись (например C:\\SQLBackup на диске сервера), либо общую сетевую папку с правами для учётной записи SQL Server.";
            }
            else if (msg.IndexOf("failed to create", StringComparison.OrdinalIgnoreCase) >= 0 && msg.IndexOf("media family", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hint = "\n\nПроверьте, что путь к файлу корректен и диск не переполнен.";
            }

            return msg + hint;
        }

        private static string ExtractInitialCatalog(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null;
            try
            {
                var b = new SqlConnectionStringBuilder(connectionString);
                if (!string.IsNullOrWhiteSpace(b.InitialCatalog))
                    return b.InitialCatalog.Trim();
            }
            catch
            {
                // строка может быть в нестандартном формате — ниже regex
            }

            var m1 = Regex.Match(connectionString, @"Initial\s*Catalog\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            if (m1.Success)
                return m1.Groups[1].Value.Trim();

            var m2 = Regex.Match(connectionString, @"Database\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
            return m2.Success ? m2.Groups[1].Value.Trim() : null;
        }
    }
}

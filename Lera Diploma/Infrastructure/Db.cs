using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Lera_Diploma.Infrastructure
{
    /// <summary>Доступ к SQL как в BGSK1 (ADO.NET поверх строки подключения приложения).</summary>
    public static class Db
    {
        public const string AppConnectionName = "FinanceContext";

        public static string AppConnectionString =>
            ConfigurationManager.ConnectionStrings[AppConnectionName]?.ConnectionString;

        public static object ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(AppConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                    command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(AppConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                    command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(AppConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                if (parameters != null && parameters.Length > 0)
                    command.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(command))
                {
                    var table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }
    }
}

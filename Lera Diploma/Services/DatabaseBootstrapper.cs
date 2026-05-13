using System.Data.Entity;
using Lera_Diploma.Data;

namespace Lera_Diploma.Services
{
    /// <summary>Инициализация БД при запуске (аналог BGSK1.DatabaseBootstrapper).</summary>
    public static class DatabaseBootstrapper
    {
        public static void EnsureDatabase()
        {
            Database.SetInitializer(new FinancialDbInitializer());
            using (var db = new FinancialDbContext())
            {
                db.Database.Initialize(true);
            }

            TestDataSeeder.EnsureRichDemoData();
            ReportHeaderService.EnsureDefaultSettings();
        }
    }
}

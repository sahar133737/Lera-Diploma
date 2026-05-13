using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;

namespace Lera_Diploma.Services
{
    /// <summary>Реквизиты организации для шапки отчётов (AppSettings).</summary>
    public static class ReportHeaderService
    {
        public const string KeyOrgName = "Report.OrgName";
        public const string KeyOrgInn = "Report.OrgInn";
        public const string KeyOrgKpp = "Report.OrgKpp";
        public const string KeyOrgAddress = "Report.OrgAddress";
        public const string KeyOrgSigner = "Report.OrgSigner";

        public static IReadOnlyList<string> GetHeaderLines()
        {
            using (var db = new FinancialDbContext())
            {
                string V(string key) => db.AppSettings.AsNoTracking().FirstOrDefault(x => x.Key == key)?.Value?.Trim();

                var lines = new List<string>();
                var name = V(KeyOrgName);
                if (!string.IsNullOrEmpty(name))
                    lines.Add(name);
                var inn = V(KeyOrgInn);
                var kpp = V(KeyOrgKpp);
                if (!string.IsNullOrEmpty(inn) || !string.IsNullOrEmpty(kpp))
                    lines.Add("ИНН " + (inn ?? "—") + "   КПП " + (kpp ?? "—"));
                var addr = V(KeyOrgAddress);
                if (!string.IsNullOrEmpty(addr))
                    lines.Add(addr);
                var signer = V(KeyOrgSigner);
                if (!string.IsNullOrEmpty(signer))
                    lines.Add(signer);
                return lines;
            }
        }

        /// <summary>Добавляет значения по умолчанию, если ключей ещё нет (вызывать при старте приложения).</summary>
        public static void EnsureDefaultSettings()
        {
            using (var db = new FinancialDbContext())
            {
                void Add(string key, string value)
                {
                    if (db.AppSettings.Any(x => x.Key == key))
                        return;
                    db.AppSettings.Add(new AppSetting { Key = key, Value = value });
                }

                Add(KeyOrgName, "Финансовый отдел администрации муниципального района «Дубровский» Брянской области");
                Add(KeyOrgInn, "3232000000");
                Add(KeyOrgKpp, "323201001");
                Add(KeyOrgAddress, "пгт Дубровский, ул. Центральная, д. 1");
                Add(KeyOrgSigner, "Главный бухгалтер ____________________ / Иванова О.П. /");
                db.SaveChanges();
            }
        }
    }
}

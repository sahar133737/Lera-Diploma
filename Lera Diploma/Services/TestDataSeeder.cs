using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;

namespace Lera_Diploma.Services
{
    /// <summary>Добавляет объёмные демо-данные (~50 строк в ключевых сущностях) без очистки БД.</summary>
    public static class TestDataSeeder
    {
        private const int TargetCount = 50;

        public static void EnsureRichDemoData()
        {
            using (var db = new FinancialDbContext())
            {
                if (!db.Roles.Any())
                    return;

                if (db.Counterparties.Count() >= TargetCount &&
                    db.BudgetItems.Count() >= TargetCount &&
                    db.FinancialDocuments.Count() >= TargetCount &&
                    db.AuditLogs.Count() >= TargetCount)
                    return;

                var rnd = new Random(20260513);
                var admin = db.Users.OrderBy(x => x.Id).First();
                var posted = db.DocumentStatuses.First(x => x.Code == "Posted");
                var draft = db.DocumentStatuses.First(x => x.Code == "Draft");
                var types = db.DocumentTypes.OrderBy(x => x.Id).ToList();
                var accounts = db.Accounts.OrderBy(x => x.Id).ToList();
                if (accounts.Count < 2)
                    return;

                ExpandAccounts(db, rnd, ref accounts);
                ExpandCounterparties(db, rnd);
                ExpandBudgetItems(db, rnd);
                ExpandDocuments(db, rnd, admin, posted, draft, types, accounts);
                ExpandAudit(db, rnd, admin.Id);

                db.SaveChanges();
            }
        }

        private static void ExpandAccounts(FinancialDbContext db, Random rnd, ref List<Account> accounts)
        {
            var codes = new HashSet<string>(db.Accounts.Select(x => x.Code));
            var templates = new[]
            {
                ("1110", "Денежные средства в кассе"),
                ("1210", "Финансовые вложения"),
                ("1310", "НДС по приобретённым ценностям"),
                ("2050", "Расчёты с прочими дебиторами"),
                ("3020", "Расчёты с подотчётными лицами"),
                ("4010", "Расходы текущего финансового года"),
                ("4011", "Расходы на содержание имущества"),
                ("4012", "Коммунальные расходы"),
                ("4013", "Транспортные расходы"),
                ("4014", "Расходы на связь и ИТ"),
                ("4015", "Прочие расходы"),
                ("2010", "Расчёты с поставщиками и подрядчиками"),
                ("2080", "Расчёты с разными дебиторами и кредиторами")
            };
            foreach (var t in templates)
            {
                if (codes.Contains(t.Item1))
                    continue;
                db.Accounts.Add(new Account { Code = t.Item1, Name = t.Item2, ParentAccountId = null });
                codes.Add(t.Item1);
            }
            db.SaveChanges();
            accounts = db.Accounts.OrderBy(x => x.Id).ToList();
        }

        private static void ExpandCounterparties(FinancialDbContext db, Random rnd)
        {
            var orgNames = new[]
            {
                "ООО «Дубровские коммуникации»", "АО «БрянскВодоканал»", "ООО «РегионСтрой»", "АО «ТехноСервис»",
                "ООО «ГарантАудит»", "ИП Соловьёв А.Н.", "ООО «УчебКомплект»", "АО «МедиаПро»",
                "ООО «ТеплоСнаб»", "МУП «Дубровское ЖКХ»", "ООО «Светофор»", "АО «АгроХолдинг»",
                "ООО «КанцПлюс»", "ООО «ИТ-Решения»", "ООО «Безопасность»", "ООО «КлинингПрофи»",
                "ООО «ТрансЛогистик»", "АО «СтройМатериалы»", "ООО «Охрана»", "ООО «Питание»",
                "ООО «Сад и парк»", "ООО «ЭнергоСервис»", "ООО «РемонтДом»", "ООО «КультураСервис»",
                "ООО «СпортИнвентарь»", "ООО «Медицина+»", "ООО «Образование»", "ООО «СвязьРегион»",
                "ООО «ПожБезопасность»", "ООО «ЛифтСервис»", "ООО «ДорСтрой»", "ООО «Водитель»",
                "ООО «Уборка»", "ООО «ОкнаПлюс»", "ООО «Кровля»", "ООО «Отопление»",
                "ООО «Вентиляция»", "ООО «Освещение»", "ООО «Слаботочка»", "ООО «СКУД»",
                "ООО «Видеонаблюдение»", "ООО «Серверная»", "ООО «1С-Сопровождение»", "ООО «Печать»",
                "ООО «Копицентр»", "ООО «Мебель»", "ООО «ХозТовары»", "ООО «ГСМ»",
                "ООО «АвтоСервис»", "ООО «Шины»", "ООО «Аренда»", "ООО «Страхование»",
                "ООО «ЮрСопровождение»", "ООО «Кадры»", "ООО «Оценка»", "ООО «Экспертиза»"
            };

            var n = db.Counterparties.Count();
            var idx = 0;
            while (n < TargetCount)
            {
                var name = orgNames[idx % orgNames.Length] + (idx >= orgNames.Length ? $" ({idx})" : "");
                var innNum = 3232000100L + idx;
                var inn = innNum.ToString("0000000000");
                if (inn.Length > 10)
                    inn = inn.Substring(0, 10);
                if (db.Counterparties.Any(x => x.Inn == inn))
                {
                    idx++;
                    continue;
                }
                var kpp = inn.Length >= 4 ? inn.Substring(0, 4) + "05001" : "323401001";
                db.Counterparties.Add(new Counterparty
                {
                    Name = name,
                    Inn = inn,
                    Kpp = kpp,
                    Kind = "ЮЛ"
                });
                n++;
                idx++;
            }
            db.SaveChanges();
        }

        private static void ExpandBudgetItems(FinancialDbContext db, Random rnd)
        {
            var prefixes = new[] { "244010000120243", "244010000120253", "244010000120213", "244010000120223", "244010000120233" };
            var n = db.BudgetItems.Count();
            var seq = 0;
            while (n < TargetCount)
            {
                var p = prefixes[rnd.Next(prefixes.Length)];
                var code = p + (30030 + seq).ToString("D6");
                seq++;
                if (db.BudgetItems.Any(x => x.Code == code))
                    continue;
                db.BudgetItems.Add(new BudgetItem
                {
                    Code = code,
                    Name = SampleBudgetName(rnd, seq)
                });
                n++;
            }
            db.SaveChanges();
        }

        private static string SampleBudgetName(Random rnd, int i)
        {
            var parts = new[]
            {
                "Коммунальные услуги", "Электроснабжение", "Водоснабжение", "Теплоснабжение", "Вывоз ТКО",
                "Текущий ремонт", "Капитальный ремонт", "Закупка ТМЦ", "Услуги связи", "Интернет",
                "Программное обеспечение", "Охрана", "Видеонаблюдение", "Страхование", "Транспортные услуги",
                "ГСМ", "Канцелярия", "Медицинское обслуживание", "Обучение персонала", "Прочие услуги"
            };
            return parts[rnd.Next(parts.Length)] + $" (КБК {i:000})";
        }

        private static void ExpandDocuments(
            FinancialDbContext db,
            Random rnd,
            User admin,
            DocumentStatus posted,
            DocumentStatus draft,
            List<DocumentType> types,
            List<Account> accounts)
        {
            var cps = db.Counterparties.OrderBy(x => x.Id).ToList();
            var budgets = db.BudgetItems.OrderBy(x => x.Id).ToList();
            if (types.Count == 0 || cps.Count == 0)
                return;
            var debit = accounts.FirstOrDefault(x => x.Code == "4010")
                ?? accounts.FirstOrDefault(x => x.Code.StartsWith("401"))
                ?? accounts[0];
            var credit = accounts.FirstOrDefault(x => x.Code == "1010")
                ?? accounts.FirstOrDefault(x => x.Code == "1110")
                ?? accounts.FirstOrDefault(x => x.Code == "2010")
                ?? accounts[Math.Min(1, accounts.Count - 1)];

            var n = db.FinancialDocuments.Count();
            var year = DateTime.Today.Year;
            var seq = db.FinancialDocuments.Count(x => x.Number.StartsWith("ДБР-" + year)) + 1;

            while (n < TargetCount)
            {
                var isPosted = rnd.NextDouble() < 0.72;
                var status = isPosted ? posted : draft;
                var day = DateTime.Today.AddDays(-rnd.Next(0, 380));
                var num = $"ДБР-{year}/{seq:0000}";
                seq++;
                if (db.FinancialDocuments.Any(x => x.Number == num))
                    continue;

                var doc = new FinancialDocument
                {
                    Number = num,
                    DocumentDate = day,
                    DocumentTypeId = types[rnd.Next(types.Count)].Id,
                    DocumentStatusId = status.Id,
                    CounterpartyId = cps[rnd.Next(cps.Count)].Id,
                    ResponsibleUserId = admin.Id,
                    Comment = SampleComment(rnd)
                };
                db.FinancialDocuments.Add(doc);
                db.SaveChanges();

                if (isPosted || rnd.NextDouble() < 0.55)
                {
                    var amount = Math.Round((decimal)(5000 + rnd.NextDouble() * 450_000), 2);
                    db.AccountingEntries.Add(new AccountingEntry
                    {
                        FinancialDocumentId = doc.Id,
                        DebitAccountId = debit.Id,
                        CreditAccountId = credit.Id,
                        Amount = amount,
                        LineNo = 1,
                        Purpose = SamplePurpose(rnd)
                    });
                    if (isPosted && budgets.Count > 0)
                    {
                        db.DocumentBudgetAllocations.Add(new DocumentBudgetAllocation
                        {
                            FinancialDocumentId = doc.Id,
                            BudgetItemId = budgets[rnd.Next(budgets.Count)].Id,
                            Amount = amount
                        });
                    }
                }
                n++;
            }
            db.SaveChanges();
        }

        private static string SampleComment(Random rnd)
        {
            var c = new[]
            {
                "Оплата по договору поставки", "Аванс по договору", "Услуги за отчётный период", "Закупка расходных материалов",
                "Подписка на ПО", "Техническое обслуживание", "Аренда помещений", "Коммунальные платежи",
                "Транспортные расходы", "Страховая премия", "Канцелярские товары", "Ремонт оборудования"
            };
            return c[rnd.Next(c.Length)];
        }

        private static string SamplePurpose(Random rnd)
        {
            var c = new[]
            {
                "Оплата поставщику", "Услуги связи", "Электроэнергия", "Водоснабжение", "ТКО", "ТО оборудования",
                "Подписка", "Аренда", "ГСМ", "Закупка ТМЦ", "Работы по договору"
            };
            return c[rnd.Next(c.Length)];
        }

        private static void ExpandAudit(FinancialDbContext db, Random rnd, int adminUserId)
        {
            var n = db.AuditLogs.Count();
            var actions = new[] { "View", "Export", "Print", "Search", "Open", "Navigate", "Filter", "Report" };
            var entities = new[] { "FinancialDocument", "Report", "Counterparty", "BudgetItem", "Account", "User" };
            var seq = 0;
            while (n < TargetCount)
            {
                db.AuditLogs.Add(new AuditLog
                {
                    UserId = adminUserId,
                    Action = actions[rnd.Next(actions.Length)],
                    EntityType = entities[rnd.Next(entities.Length)],
                    EntityKey = rnd.Next(1, 9999).ToString(),
                    Details = "Демо-запись журнала аудита #" + (++seq),
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-rnd.Next(0, 60 * 24 * 120))
                });
                n++;
            }
            db.SaveChanges();
        }
    }
}

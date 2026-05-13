using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;

namespace Lera_Diploma.Services
{
    /// <summary>Сложные отчёты: группировки и суммы.</summary>
    public sealed class ReportingService
    {
        /// <summary>Обороты по счёту за период (по проводкам проведённых документов).</summary>
        public DataTable ReportAccountTurnover(int accountId, DateTime from, DateTime to)
        {
            using (var db = new FinancialDbContext())
            {
                var entries = db.AccountingEntries
                    .Include("DebitAccount")
                    .Include("CreditAccount")
                    .Include("FinancialDocument.DocumentStatus")
                    .Where(x => x.FinancialDocument.DocumentDate >= from && x.FinancialDocument.DocumentDate <= to)
                    .Where(x => x.FinancialDocument.DocumentStatus.Code == "Posted")
                    .Where(x => x.DebitAccountId == accountId || x.CreditAccountId == accountId)
                    .ToList();

                var dt = new DataTable();
                dt.Columns.Add("Дата", typeof(DateTime));
                dt.Columns.Add("Документ");
                dt.Columns.Add("Дебет");
                dt.Columns.Add("Кредит");
                dt.Columns.Add("Сумма", typeof(decimal));
                dt.Columns.Add("Назначение");

                decimal turnDebit = 0, turnCredit = 0;
                foreach (var e in entries.OrderBy(x => x.FinancialDocument.DocumentDate))
                {
                    var row = dt.NewRow();
                    row["Дата"] = e.FinancialDocument.DocumentDate;
                    row["Документ"] = e.FinancialDocument.Number;
                    row["Дебет"] = e.DebitAccount.Code;
                    row["Кредит"] = e.CreditAccount.Code;
                    row["Сумма"] = e.Amount;
                    row["Назначение"] = e.Purpose ?? "";
                    dt.Rows.Add(row);
                    if (e.DebitAccountId == accountId)
                        turnDebit += e.Amount;
                    if (e.CreditAccountId == accountId)
                        turnCredit += e.Amount;
                }

                var total = dt.NewRow();
                total["Документ"] = "ИТОГО оборот Дт / Кт по счёту";
                total["Дебет"] = turnDebit.ToString("N2");
                total["Кредит"] = turnCredit.ToString("N2");
                total["Сумма"] = (turnDebit - turnCredit);
                dt.Rows.Add(total);
                return dt;
            }
        }

        /// <summary>Реестр проводок с группировкой по месяцу и контрагенту.</summary>
        public DataTable ReportEntriesByMonthAndCounterparty(DateTime fromDate, DateTime toDate)
        {
            using (var db = new FinancialDbContext())
            {
                var raw = (from e in db.AccountingEntries
                             join d in db.FinancialDocuments on e.FinancialDocumentId equals d.Id
                             join s in db.DocumentStatuses on d.DocumentStatusId equals s.Id
                             where d.DocumentDate >= fromDate && d.DocumentDate <= toDate && s.Code == "Posted"
                             select new
                             {
                                 d.DocumentDate,
                                 d.Number,
                                 CounterpartyName = d.Counterparty != null ? d.Counterparty.Name : "(без контрагента)",
                                 e.Amount
                             }).ToList();

                var grouped = raw
                    .GroupBy(x => new { MonthKey = x.DocumentDate.Year * 100 + x.DocumentDate.Month, x.CounterpartyName })
                    .Select(g => new
                    {
                        g.Key.MonthKey,
                        g.Key.CounterpartyName,
                        Sum = g.Sum(x => x.Amount),
                        Lines = g.Count()
                    })
                    .OrderBy(x => x.MonthKey)
                    .ThenBy(x => x.CounterpartyName)
                    .ToList();

                var dt = new DataTable();
                dt.Columns.Add("Месяц");
                dt.Columns.Add("Контрагент");
                dt.Columns.Add("Строк", typeof(int));
                dt.Columns.Add("Сумма", typeof(decimal));

                foreach (var x in grouped)
                    dt.Rows.Add($"{x.MonthKey / 100:0000}-{x.MonthKey % 100:00}", x.CounterpartyName, x.Lines, x.Sum);

                if (grouped.Any())
                    dt.Rows.Add("ВСЕГО", "", grouped.Sum(x => x.Lines), grouped.Sum(x => x.Sum));

                return dt;
            }
        }

        /// <summary>Свод по статьям бюджета (аллокации по проведённым документам).</summary>
        public DataTable ReportBudgetSummary(DateTime fromDate, DateTime toDate)
        {
            using (var db = new FinancialDbContext())
            {
                var rows = (from a in db.DocumentBudgetAllocations
                            join d in db.FinancialDocuments on a.FinancialDocumentId equals d.Id
                            join s in db.DocumentStatuses on d.DocumentStatusId equals s.Id
                            join b in db.BudgetItems on a.BudgetItemId equals b.Id
                            where d.DocumentDate >= fromDate && d.DocumentDate <= toDate && s.Code == "Posted"
                            select new { b.Code, b.Name, a.Amount }).ToList();

                var list = rows.GroupBy(x => new { x.Code, x.Name })
                    .Select(g => new { g.Key.Code, g.Key.Name, Sum = g.Sum(x => x.Amount) })
                    .OrderBy(x => x.Code)
                    .ToList();

                var dt = new DataTable();
                dt.Columns.Add("Код статьи");
                dt.Columns.Add("Наименование");
                dt.Columns.Add("Сумма", typeof(decimal));
                foreach (var x in list)
                    dt.Rows.Add(x.Code, x.Name, x.Sum);
                if (list.Any())
                    dt.Rows.Add("", "ИТОГО", list.Sum(x => x.Sum));
                return dt;
            }
        }

        /// <summary>
        /// Сложный свод: аллокации бюджета по проведённым документам с разрезом контрагент × тип документа × статья бюджета
        /// (с количеством уникальных документов и суммой аллокаций).
        /// </summary>
        public DataTable ReportBudgetAllocationsByCounterpartyDocTypeAndArticle(DateTime fromDate, DateTime toDate)
        {
            using (var db = new FinancialDbContext())
            {
                var raw = (from a in db.DocumentBudgetAllocations
                            join d in db.FinancialDocuments on a.FinancialDocumentId equals d.Id
                            join s in db.DocumentStatuses on d.DocumentStatusId equals s.Id
                            join t in db.DocumentTypes on d.DocumentTypeId equals t.Id
                            join b in db.BudgetItems on a.BudgetItemId equals b.Id
                            where d.DocumentDate >= fromDate && d.DocumentDate <= toDate && s.Code == "Posted"
                            select new
                            {
                                CounterpartyName = d.Counterparty != null ? d.Counterparty.Name : "(без контрагента)",
                                DocType = t.Name,
                                b.Code,
                                ArticleName = b.Name,
                                d.Id,
                                a.Amount
                            }).ToList();

                var grouped = raw
                    .GroupBy(x => new { x.CounterpartyName, x.DocType, x.Code, x.ArticleName })
                    .Select(g => new
                    {
                        g.Key.CounterpartyName,
                        g.Key.DocType,
                        g.Key.Code,
                        g.Key.ArticleName,
                        DocCount = g.Select(x => x.Id).Distinct().Count(),
                        Sum = g.Sum(x => x.Amount)
                    })
                    .OrderByDescending(x => x.Sum)
                    .ThenBy(x => x.CounterpartyName)
                    .ToList();

                var dt = new DataTable();
                dt.Columns.Add("Контрагент");
                dt.Columns.Add("Тип документа");
                dt.Columns.Add("Код статьи");
                dt.Columns.Add("Статья бюджета");
                dt.Columns.Add("Документов", typeof(int));
                dt.Columns.Add("Сумма аллокаций", typeof(decimal));

                foreach (var x in grouped)
                    dt.Rows.Add(x.CounterpartyName, x.DocType, x.Code, x.ArticleName, x.DocCount, x.Sum);

                if (grouped.Any())
                    dt.Rows.Add("", "", "", "ИТОГО", 0, grouped.Sum(x => x.Sum));

                return dt;
            }
        }
    }
}

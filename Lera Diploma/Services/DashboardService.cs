using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;

namespace Lera_Diploma.Services
{
    public sealed class DashboardService
    {
        public sealed class KpiRow
        {
            public string Title { get; set; }
            public string Value { get; set; }
            public string Subtitle { get; set; }
        }

        public IReadOnlyList<KpiRow> GetKpis(DateTime fromUtcDate, DateTime toUtcDate)
        {
            using (var db = new FinancialDbContext())
            {
                var postedId = db.DocumentStatuses.AsNoTracking().First(x => x.Code == "Posted").Id;
                var draftId = db.DocumentStatuses.AsNoTracking().First(x => x.Code == "Draft").Id;

                var q = db.FinancialDocuments.AsNoTracking().Where(d => d.DocumentDate >= fromUtcDate && d.DocumentDate <= toUtcDate);
                var postedCount = q.Count(d => d.DocumentStatusId == postedId);
                var draftCount = q.Count(d => d.DocumentStatusId == draftId);

                var sumPosted = (from d in db.FinancialDocuments.AsNoTracking()
                                 where d.DocumentStatusId == postedId && d.DocumentDate >= fromUtcDate && d.DocumentDate <= toUtcDate
                                 join e in db.AccountingEntries.AsNoTracking() on d.Id equals e.FinancialDocumentId
                                 select (decimal?)e.Amount).Sum() ?? 0;

                var cpCount = (from d in db.FinancialDocuments.AsNoTracking()
                               where d.DocumentDate >= fromUtcDate && d.DocumentDate <= toUtcDate && d.CounterpartyId != null
                               select d.CounterpartyId).Distinct().Count();

                return new[]
                {
                    new KpiRow { Title = "Проведённые документы", Value = postedCount.ToString("N0"), Subtitle = "за выбранный период" },
                    new KpiRow { Title = "Черновики", Value = draftCount.ToString("N0"), Subtitle = "требуют проведения" },
                    new KpiRow { Title = "Оборот по проводкам", Value = sumPosted.ToString("N2") + " ₽", Subtitle = "по проведённым" },
                    new KpiRow { Title = "Контрагентов в обороте", Value = cpCount.ToString("N0"), Subtitle = "уникальных в периоде" }
                };
            }
        }

        /// <summary>Суммы по дням (проведённые).</summary>
        public IReadOnlyList<KeyValuePair<DateTime, decimal>> GetDailyPostedAmounts(DateTime fromDate, DateTime toDate)
        {
            using (var db = new FinancialDbContext())
            {
                var postedId = db.DocumentStatuses.AsNoTracking().First(x => x.Code == "Posted").Id;
                var rows = from d in db.FinancialDocuments.AsNoTracking()
                             where d.DocumentStatusId == postedId && d.DocumentDate >= fromDate && d.DocumentDate <= toDate
                             join e in db.AccountingEntries.AsNoTracking() on d.Id equals e.FinancialDocumentId
                             group e by d.DocumentDate into g
                             select new { Day = g.Key, Sum = g.Sum(x => x.Amount) };
                return rows.OrderBy(x => x.Day).ToList().Select(x => new KeyValuePair<DateTime, decimal>(x.Day, x.Sum)).ToList();
            }
        }

        /// <summary>Суммы по типам документов (проведённые).</summary>
        public IReadOnlyList<KeyValuePair<string, decimal>> GetPostedAmountsByDocType(DateTime fromDate, DateTime toDate)
        {
            using (var db = new FinancialDbContext())
            {
                var postedId = db.DocumentStatuses.AsNoTracking().First(x => x.Code == "Posted").Id;
                var rows = from d in db.FinancialDocuments.AsNoTracking()
                             where d.DocumentStatusId == postedId && d.DocumentDate >= fromDate && d.DocumentDate <= toDate
                             join t in db.DocumentTypes.AsNoTracking() on d.DocumentTypeId equals t.Id
                             join e in db.AccountingEntries.AsNoTracking() on d.Id equals e.FinancialDocumentId
                             group e by t.Name into g
                             select new { Name = g.Key, Sum = g.Sum(x => x.Amount) };
                return rows.OrderByDescending(x => x.Sum).ToList().Select(x => new KeyValuePair<string, decimal>(x.Name, x.Sum)).ToList();
            }
        }

        public object GetRecentDocuments(int take = 15)
        {
            using (var db = new FinancialDbContext())
            {
                return db.FinancialDocuments
                    .AsNoTracking()
                    .Include("DocumentStatus")
                    .Include("DocumentType")
                    .OrderByDescending(x => x.DocumentDate)
                    .ThenByDescending(x => x.Id)
                    .Take(take)
                    .ToList()
                    .Select(x => new
                    {
                        x.Id,
                        x.Number,
                        x.DocumentDate,
                        Тип = x.DocumentType.Name,
                        Статус = x.DocumentStatus.Name
                    }).ToList();
            }
        }
    }
}

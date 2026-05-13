using System;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;

namespace Lera_Diploma.Services
{
    public sealed class LedgerService
    {
        public object GetEntriesForGrid(string search, DateTime? from, DateTime? to, int? documentStatusId)
        {
            using (var db = new FinancialDbContext())
            {
                var q =
                    from e in db.AccountingEntries
                    join d in db.FinancialDocuments on e.FinancialDocumentId equals d.Id
                    join s in db.DocumentStatuses on d.DocumentStatusId equals s.Id
                    join dt in db.DocumentTypes on d.DocumentTypeId equals dt.Id
                    join da in db.Accounts on e.DebitAccountId equals da.Id
                    join ca in db.Accounts on e.CreditAccountId equals ca.Id
                    select new { e, d, s, dt, da, ca };

                if (documentStatusId.HasValue)
                    q = q.Where(x => x.d.DocumentStatusId == documentStatusId.Value);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var t = search.Trim();
                    q = q.Where(x =>
                        (x.d.Number != null && x.d.Number.Contains(t))
                        || (x.e.Purpose != null && x.e.Purpose.Contains(t))
                        || x.s.Name.Contains(t)
                        || x.dt.Name.Contains(t)
                        || x.da.Code.Contains(t)
                        || x.ca.Code.Contains(t));
                }

                if (from.HasValue)
                    q = q.Where(x => x.d.DocumentDate >= from.Value);
                if (to.HasValue)
                    q = q.Where(x => x.d.DocumentDate <= to.Value);

                return q
                    .OrderByDescending(x => x.d.DocumentDate)
                    .ThenByDescending(x => x.e.Id)
                    .Take(5000)
                    .Select(x => new
                    {
                        x.e.Id,
                        FinancialDocumentId = x.d.Id,
                        x.d.Number,
                        x.d.DocumentDate,
                        Status = x.s.Name,
                        DocType = x.dt.Name,
                        Debit = x.da.Code,
                        Credit = x.ca.Code,
                        x.e.Amount,
                        x.e.Purpose
                    })
                    .ToList();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    public sealed class DocumentSaveModel
    {
        public int? Id { get; set; }
        public string Number { get; set; }
        public DateTime DocumentDate { get; set; }
        public int DocumentTypeId { get; set; }
        public int? CounterpartyId { get; set; }
        public int ResponsibleUserId { get; set; }
        public string Comment { get; set; }
        public List<DocumentEntrySaveRow> Entries { get; set; } = new List<DocumentEntrySaveRow>();
    }

    public sealed class DocumentEntrySaveRow
    {
        public int? Id { get; set; }
        public int DebitAccountId { get; set; }
        public int CreditAccountId { get; set; }
        public decimal Amount { get; set; }
        public int LineNo { get; set; }
        public string Purpose { get; set; }
    }

    public sealed class DocumentService
    {
        public IReadOnlyList<FinancialDocument> GetDocuments(string searchText, int? statusId, DateTime? from, DateTime? to)
        {
            using (var db = new FinancialDbContext())
            {
                var q = db.FinancialDocuments
                    .Include("DocumentType")
                    .Include("DocumentStatus")
                    .Include("Counterparty")
                    .Include("ResponsibleUser")
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var s = searchText.Trim();
                    q = q.Where(x =>
                        (x.Number != null && x.Number.Contains(s))
                        || (x.Comment != null && x.Comment.Contains(s))
                        || (x.Counterparty != null && x.Counterparty.Name.Contains(s))
                        || (x.DocumentType != null && x.DocumentType.Name.Contains(s))
                        || (x.DocumentStatus != null && x.DocumentStatus.Name.Contains(s))
                        || (x.ResponsibleUser != null && x.ResponsibleUser.FullName.Contains(s)));
                }

                if (statusId.HasValue)
                    q = q.Where(x => x.DocumentStatusId == statusId.Value);
                if (from.HasValue)
                    q = q.Where(x => x.DocumentDate >= from.Value);
                if (to.HasValue)
                    q = q.Where(x => x.DocumentDate <= to.Value);

                return q.OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.Id).ToList();
            }
        }

        public FinancialDocument GetById(int id)
        {
            using (var db = new FinancialDbContext())
            {
                return db.FinancialDocuments
                    .Include("DocumentType")
                    .Include("DocumentStatus")
                    .Include("Counterparty")
                    .Include("ResponsibleUser")
                    .Include("Entries.DebitAccount")
                    .Include("Entries.CreditAccount")
                    .FirstOrDefault(x => x.Id == id);
            }
        }

        public string TrySave(DocumentSaveModel model, out int savedId)
        {
            savedId = 0;
            if (model == null)
                return "Нет данных.";
            if (string.IsNullOrWhiteSpace(model.Number))
                return "Укажите номер документа.";
            if (model.DocumentTypeId <= 0)
                return "Выберите тип документа.";
            if (model.ResponsibleUserId <= 0)
                return "Укажите ответственного.";

            using (var db = new FinancialDbContext())
            {
                var draft = db.DocumentStatuses.First(x => x.Code == "Draft");

                FinancialDocument doc;
                if (model.Id.HasValue && model.Id.Value > 0)
                {
                    doc = db.FinancialDocuments.Include("DocumentStatus").Include("Entries").FirstOrDefault(x => x.Id == model.Id.Value);
                    if (doc == null)
                        return "Документ не найден.";
                    if (doc.DocumentStatus.Code != "Draft")
                        return "Редактировать можно только черновик.";
                }
                else
                {
                    if (db.FinancialDocuments.Any(x => x.Number == model.Number.Trim()))
                        return "Документ с таким номером уже существует.";
                    doc = new FinancialDocument
                    {
                        DocumentStatusId = draft.Id
                    };
                    db.FinancialDocuments.Add(doc);
                }

                if (db.FinancialDocuments.Any(x => x.Number == model.Number.Trim() && x.Id != doc.Id))
                    return "Номер уже занят другим документом.";

                doc.Number = model.Number.Trim();
                doc.DocumentDate = model.DocumentDate.Date;
                doc.DocumentTypeId = model.DocumentTypeId;
                doc.CounterpartyId = model.CounterpartyId;
                doc.ResponsibleUserId = model.ResponsibleUserId;
                doc.Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();

                db.SaveChanges();

                var existing = db.AccountingEntries.Where(x => x.FinancialDocumentId == doc.Id).ToList();
                foreach (var e in existing)
                    db.AccountingEntries.Remove(e);

                var line = 1;
                foreach (var row in model.Entries.OrderBy(x => x.LineNo))
                {
                    if (row.Amount <= 0)
                        return "Суммы проводок должны быть положительными.";
                    if (row.DebitAccountId == row.CreditAccountId)
                        return "В строке дебет и кредит не должны совпадать.";
                    db.AccountingEntries.Add(new AccountingEntry
                    {
                        FinancialDocumentId = doc.Id,
                        DebitAccountId = row.DebitAccountId,
                        CreditAccountId = row.CreditAccountId,
                        Amount = Math.Round(row.Amount, 2),
                        LineNo = row.LineNo > 0 ? row.LineNo : line,
                        Purpose = string.IsNullOrWhiteSpace(row.Purpose) ? null : row.Purpose.Trim()
                    });
                    line++;
                }

                db.SaveChanges();
                savedId = doc.Id;
                new AuditService().Write(CurrentUserContext.UserId, model.Id.HasValue ? "Update" : "Create", "FinancialDocument", doc.Id.ToString(), doc.Number);
                return null;
            }
        }

        public string TryDelete(int documentId)
        {
            using (var db = new FinancialDbContext())
            {
                var doc = db.FinancialDocuments.Include("DocumentStatus").Include("BudgetAllocations").FirstOrDefault(x => x.Id == documentId);
                if (doc == null)
                    return "Документ не найден.";
                if (doc.DocumentStatus.Code != "Draft")
                    return "Удалить можно только черновик.";

                foreach (var a in doc.BudgetAllocations.ToList())
                    db.DocumentBudgetAllocations.Remove(a);
                foreach (var e in db.AccountingEntries.Where(x => x.FinancialDocumentId == doc.Id).ToList())
                    db.AccountingEntries.Remove(e);
                db.FinancialDocuments.Remove(doc);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Delete", "FinancialDocument", documentId.ToString(), null);
                return null;
            }
        }

        public string TryPostDocument(int documentId, int userId, out string error)
        {
            error = null;
            using (var db = new FinancialDbContext())
            {
                var doc = db.FinancialDocuments.Include("Entries").Include("DocumentStatus").FirstOrDefault(x => x.Id == documentId);
                if (doc == null)
                {
                    error = "Документ не найден.";
                    return null;
                }

                if (doc.DocumentStatus.Code != "Draft")
                {
                    error = "Провести можно только черновик.";
                    return null;
                }

                if (!doc.Entries.Any())
                {
                    error = "Нет проводок.";
                    return null;
                }

                foreach (var line in doc.Entries)
                {
                    if (line.Amount <= 0)
                    {
                        error = "Суммы проводок должны быть положительными.";
                        return null;
                    }

                    if (line.DebitAccountId == line.CreditAccountId)
                    {
                        error = "В строке проводки дебет и кредит не должны совпадать.";
                        return null;
                    }
                }

                var posted = db.DocumentStatuses.First(x => x.Code == "Posted");
                doc.DocumentStatusId = posted.Id;
                db.SaveChanges();
                new AuditService().Write(userId, "PostDocument", "FinancialDocument", documentId.ToString(), null);
                return "Документ проведён.";
            }
        }
    }
}

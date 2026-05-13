using System;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    public sealed class ReferenceDataService
    {
        public System.Collections.Generic.List<DocumentType> GetDocumentTypes()
        {
            using (var db = new FinancialDbContext())
                return db.DocumentTypes.OrderBy(x => x.Name).ToList();
        }

        public System.Collections.Generic.List<DocumentStatus> GetDocumentStatuses()
        {
            using (var db = new FinancialDbContext())
                return db.DocumentStatuses.OrderBy(x => x.Name).ToList();
        }

        public string TryAddDocumentType(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return "Заполните код и наименование.";
            using (var db = new FinancialDbContext())
            {
                if (db.DocumentTypes.Any(x => x.Code == code.Trim()))
                    return "Тип с таким кодом уже есть.";
                db.DocumentTypes.Add(new DocumentType { Code = code.Trim(), Name = name.Trim() });
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Create", "DocumentType", code, name);
                return null;
            }
        }

        public string TryAddDocumentStatus(string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return "Заполните код и наименование.";
            using (var db = new FinancialDbContext())
            {
                if (db.DocumentStatuses.Any(x => x.Code == code.Trim()))
                    return "Статус с таким кодом уже есть.";
                db.DocumentStatuses.Add(new DocumentStatus { Code = code.Trim(), Name = name.Trim() });
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Create", "DocumentStatus", code, name);
                return null;
            }
        }

        public string TryUpdateDocumentType(int id, string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return "Заполните код и наименование.";
            using (var db = new FinancialDbContext())
            {
                var row = db.DocumentTypes.Find(id);
                if (row == null)
                    return "Не найдено.";
                if (db.DocumentTypes.Any(x => x.Code == code.Trim() && x.Id != id))
                    return "Код уже занят.";
                row.Code = code.Trim();
                row.Name = name.Trim();
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Update", "DocumentType", code, name);
                return null;
            }
        }

        public string TryDeleteDocumentType(int id)
        {
            using (var db = new FinancialDbContext())
            {
                var row = db.DocumentTypes.Include("Documents").FirstOrDefault(x => x.Id == id);
                if (row == null)
                    return "Не найдено.";
                if (row.Documents.Any())
                    return "Нельзя удалить: тип используется в документах.";
                db.DocumentTypes.Remove(row);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Delete", "DocumentType", id.ToString(), null);
                return null;
            }
        }

        public string TryUpdateDocumentStatus(int id, string code, string name)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return "Заполните код и наименование.";
            using (var db = new FinancialDbContext())
            {
                var row = db.DocumentStatuses.Find(id);
                if (row == null)
                    return "Не найдено.";
                if (db.DocumentStatuses.Any(x => x.Code == code.Trim() && x.Id != id))
                    return "Код уже занят.";
                row.Code = code.Trim();
                row.Name = name.Trim();
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Update", "DocumentStatus", code, name);
                return null;
            }
        }

        public string TryDeleteDocumentStatus(int id)
        {
            using (var db = new FinancialDbContext())
            {
                var row = db.DocumentStatuses.Include("Documents").FirstOrDefault(x => x.Id == id);
                if (row == null)
                    return "Не найдено.";
                if (row.Documents.Any())
                    return "Нельзя удалить: статус используется в документах.";
                if (row.Code == "Draft" || row.Code == "Posted" || row.Code == "Cancelled")
                    return "Нельзя удалить системный статус.";
                db.DocumentStatuses.Remove(row);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Delete", "DocumentStatus", id.ToString(), null);
                return null;
            }
        }
    }
}

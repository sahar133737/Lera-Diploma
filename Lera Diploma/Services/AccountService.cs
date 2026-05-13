using System;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    public sealed class AccountService
    {
        public System.Collections.Generic.List<Account> GetAll()
        {
            using (var db = new FinancialDbContext())
                return db.Accounts.OrderBy(x => x.Code).ToList();
        }

        public string TrySave(int? id, string code, string name, int? parentAccountId, out Account entity)
        {
            entity = null;
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return "Заполните код и наименование счёта.";
            code = code.Trim();
            name = name.Trim();

            using (var db = new FinancialDbContext())
            {
                if (db.Accounts.Any(x => x.Code == code && (!id.HasValue || x.Id != id.Value)))
                    return "Счёт с таким кодом уже существует.";

                if (parentAccountId.HasValue)
                {
                    var p = db.Accounts.Find(parentAccountId.Value);
                    if (p == null)
                        return "Родительский счёт не найден.";
                    if (id.HasValue && id.Value == parentAccountId.Value)
                        return "Счёт не может быть родителем самого себя.";
                }

                Account row;
                if (id.HasValue && id.Value > 0)
                {
                    row = db.Accounts.Find(id.Value);
                    if (row == null)
                        return "Не найдено.";
                }
                else
                {
                    row = new Account();
                    db.Accounts.Add(row);
                }

                row.Code = code;
                row.Name = name;
                row.ParentAccountId = parentAccountId;
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, id.HasValue ? "Update" : "Create", "Account", row.Id.ToString(), code);
                entity = row;
                return null;
            }
        }

        public string TryDelete(int id)
        {
            using (var db = new FinancialDbContext())
            {
                var row = db.Accounts.Include("DebitEntries").Include("CreditEntries").Include("Children").FirstOrDefault(x => x.Id == id);
                if (row == null)
                    return "Не найдено.";
                if (row.Children.Any())
                    return "Нельзя удалить: есть дочерние счета.";
                if (row.DebitEntries.Any() || row.CreditEntries.Any())
                    return "Нельзя удалить: счёт используется в проводках.";
                db.Accounts.Remove(row);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Delete", "Account", id.ToString(), null);
                return null;
            }
        }
    }
}

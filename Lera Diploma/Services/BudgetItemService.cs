using System;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    public sealed class BudgetItemService
    {
        public System.Collections.Generic.List<BudgetItem> GetAll()
        {
            using (var db = new FinancialDbContext())
                return db.BudgetItems.OrderBy(x => x.Code).ToList();
        }

        public string TrySave(int? id, string code, string name, out BudgetItem entity)
        {
            entity = null;
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return "Заполните код и наименование.";
            code = code.Trim();
            name = name.Trim();
            if (code.Length > 64)
                return "Код слишком длинный.";

            using (var db = new FinancialDbContext())
            {
                if (db.BudgetItems.Any(x => x.Code == code && (!id.HasValue || x.Id != id.Value)))
                    return "Код статьи уже существует.";

                BudgetItem row;
                if (id.HasValue && id.Value > 0)
                {
                    row = db.BudgetItems.Find(id.Value);
                    if (row == null)
                        return "Не найдено.";
                }
                else
                {
                    row = new BudgetItem();
                    db.BudgetItems.Add(row);
                }

                row.Code = code;
                row.Name = name;
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, id.HasValue ? "Update" : "Create", "BudgetItem", row.Id.ToString(), code);
                entity = row;
                return null;
            }
        }

        public string TryDelete(int id)
        {
            using (var db = new FinancialDbContext())
            {
                var row = db.BudgetItems.Include("Allocations").FirstOrDefault(x => x.Id == id);
                if (row == null)
                    return "Не найдено.";
                if (row.Allocations.Any())
                    return "Нельзя удалить: статья используется в документах.";
                db.BudgetItems.Remove(row);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Delete", "BudgetItem", id.ToString(), null);
                return null;
            }
        }
    }
}

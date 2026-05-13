using System;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using Lera_Diploma.Data;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Services
{
    public sealed class CounterpartyService
    {
        public System.Collections.Generic.List<Counterparty> GetAll()
        {
            using (var db = new FinancialDbContext())
                return db.Counterparties.OrderBy(x => x.Name).ToList();
        }

        public string TrySave(int? id, string name, string inn, string kpp, string kind, out Counterparty entity)
        {
            entity = null;
            if (string.IsNullOrWhiteSpace(name))
                return "Укажите наименование.";
            if (string.IsNullOrWhiteSpace(kind) || (kind != "ЮЛ" && kind != "ИП" && kind != "ФЛ"))
                return "Тип должен быть ЮЛ, ИП или ФЛ.";

            inn = inn?.Trim();
            kpp = kpp?.Trim();
            if (!string.IsNullOrEmpty(inn) && !Regex.IsMatch(inn, @"^\d{10}$|^\d{12}$"))
                return "ИНН: 10 цифр для ЮЛ или 12 для физлица.";
            if (!string.IsNullOrEmpty(kpp) && !Regex.IsMatch(kpp, @"^\d{9}$"))
                return "КПП должен содержать 9 цифр.";

            using (var db = new FinancialDbContext())
            {
                Counterparty row;
                if (id.HasValue && id.Value > 0)
                {
                    row = db.Counterparties.Find(id.Value);
                    if (row == null)
                        return "Запись не найдена.";
                }
                else
                {
                    row = new Counterparty();
                    db.Counterparties.Add(row);
                }

                row.Name = name.Trim();
                row.Inn = string.IsNullOrEmpty(inn) ? null : inn;
                row.Kpp = string.IsNullOrEmpty(kpp) ? null : kpp;
                row.Kind = kind;
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, id.HasValue ? "Update" : "Create", "Counterparty", row.Id.ToString(), row.Name);
                entity = row;
                return null;
            }
        }

        public string TryDelete(int id)
        {
            using (var db = new FinancialDbContext())
            {
                var row = db.Counterparties.Include("Documents").FirstOrDefault(x => x.Id == id);
                if (row == null)
                    return "Не найдено.";
                if (row.Documents.Any())
                    return "Нельзя удалить: есть связанные документы.";
                db.Counterparties.Remove(row);
                db.SaveChanges();
                new AuditService().Write(CurrentUserContext.UserId, "Delete", "Counterparty", id.ToString(), null);
                return null;
            }
        }
    }
}

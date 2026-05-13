using System;
using System.Data.Entity;
using System.Linq;
using Lera_Diploma.Models;
using Lera_Diploma.Security;

namespace Lera_Diploma.Data
{
    public class FinancialDbInitializer : CreateDatabaseIfNotExists<FinancialDbContext>
    {
        protected override void Seed(FinancialDbContext context)
        {
            if (context.Roles.Any())
                return;

            var roles = new[]
            {
                new Role { Code = "Admin", Name = "Администратор" },
                new Role { Code = "ChiefAccountant", Name = "Главный бухгалтер" },
                new Role { Code = "Accountant", Name = "Бухгалтер" },
                new Role { Code = "Viewer", Name = "Наблюдатель" }
            };
            context.Roles.AddRange(roles);
            context.SaveChanges();

            string Hash(string pwd) => PasswordHasher.HashPassword(pwd);

            var users = new[]
            {
                new User { Login = "admin", PasswordHash = Hash("admin"), FullName = "Системный администратор", IsActive = true },
                new User { Login = "gbuh", PasswordHash = Hash("demo"), FullName = "Иванова Ольга Петровна", IsActive = true },
                new User { Login = "buh1", PasswordHash = Hash("demo"), FullName = "Смирнов Алексей Викторович", IsActive = true },
                new User { Login = "viewer", PasswordHash = Hash("demo"), FullName = "Петрова Мария Сергеевна", IsActive = true }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            void AddUserRole(string login, string roleCode)
            {
                var u = context.Users.Local.First(x => x.Login == login);
                var r = context.Roles.Local.First(x => x.Code == roleCode);
                context.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = r.Id });
            }

            AddUserRole("admin", "Admin");
            AddUserRole("gbuh", "ChiefAccountant");
            AddUserRole("buh1", "Accountant");
            AddUserRole("viewer", "Viewer");
            context.SaveChanges();

            var statuses = new[]
            {
                new DocumentStatus { Code = "Draft", Name = "Черновик" },
                new DocumentStatus { Code = "Posted", Name = "Проведён" },
                new DocumentStatus { Code = "Cancelled", Name = "Отменён" }
            };
            context.DocumentStatuses.AddRange(statuses);

            var docTypes = new[]
            {
                new DocumentType { Code = "PAY_ORD", Name = "Платёжное поручение" },
                new DocumentType { Code = "MEM_ORDER", Name = "Бухгалтерская справка" },
                new DocumentType { Code = "INV_IN", Name = "Входящий счёт" }
            };
            context.DocumentTypes.AddRange(docTypes);
            context.SaveChanges();

            var acc1010 = new Account { Code = "1010", Name = "Денежные средства на счетах в кредитных организациях" };
            var acc2010 = new Account { Code = "2010", Name = "Расчёты с поставщиками и подрядчиками" };
            var acc4010 = new Account { Code = "4010", Name = "Расходы текущего финансового года" };
            context.Accounts.Add(acc1010);
            context.Accounts.Add(acc2010);
            context.Accounts.Add(acc4010);
            context.SaveChanges();

            var budgetItems = new[]
            {
                new BudgetItem { Code = "2440100001202430030", Name = "Коммунальные услуги (КБК пример)" },
                new BudgetItem { Code = "2440100001202530030", Name = "Прочая закупка товаров, работ и услуг" },
                new BudgetItem { Code = "2440100001202130030", Name = "Иные закупки услуг связи" }
            };
            context.BudgetItems.AddRange(budgetItems);

            var counterparties = new[]
            {
                new Counterparty { Name = "ООО «БрянскЭнерго»", Inn = "3234012345", Kpp = "323401001", Kind = "ЮЛ" },
                new Counterparty { Name = "АО «РТК»", Inn = "7707049388", Kpp = "770701001", Kind = "ЮЛ" },
                new Counterparty { Name = "ИП Кузнецов Д.В.", Inn = "320112345678", Kpp = null, Kind = "ИП" },
                new Counterparty { Name = "МБОУ СОШ №1 п. Дубровский", Inn = "3232004567", Kpp = "323201001", Kind = "ЮЛ" },
                new Counterparty { Name = "Администрация Дубровского района", Inn = "3232007890", Kpp = "323201001", Kind = "ЮЛ" }
            };
            context.Counterparties.AddRange(counterparties);
            context.SaveChanges();

            var adminUser = context.Users.Local.First(x => x.Login == "admin");
            var buhUser = context.Users.Local.First(x => x.Login == "buh1");
            var posted = context.DocumentStatuses.Local.First(x => x.Code == "Posted");
            var draft = context.DocumentStatuses.Local.First(x => x.Code == "Draft");
            var payOrd = context.DocumentTypes.Local.First(x => x.Code == "PAY_ORD");
            var memOrder = context.DocumentTypes.Local.First(x => x.Code == "MEM_ORDER");

            var docs = new[]
            {
                new FinancialDocument
                {
                    Number = "ДБР-2025/001",
                    DocumentDate = new DateTime(2025, 1, 15),
                    DocumentTypeId = payOrd.Id,
                    DocumentStatusId = posted.Id,
                    CounterpartyId = counterparties[0].Id,
                    ResponsibleUserId = buhUser.Id,
                    Comment = "Оплата электроэнергии январь"
                },
                new FinancialDocument
                {
                    Number = "ДБР-2025/002",
                    DocumentDate = new DateTime(2025, 2, 10),
                    DocumentTypeId = payOrd.Id,
                    DocumentStatusId = posted.Id,
                    CounterpartyId = counterparties[1].Id,
                    ResponsibleUserId = buhUser.Id,
                    Comment = "Услуги связи"
                },
                new FinancialDocument
                {
                    Number = "ДБР-2025/003",
                    DocumentDate = new DateTime(2025, 3, 5),
                    DocumentTypeId = memOrder.Id,
                    DocumentStatusId = draft.Id,
                    CounterpartyId = counterparties[2].Id,
                    ResponsibleUserId = adminUser.Id,
                    Comment = "Черновик: закупка канцтоваров"
                }
            };
            context.FinancialDocuments.AddRange(docs);
            context.SaveChanges();

            var d1 = docs[0];
            var d2 = docs[1];
            var d3 = docs[2];

            context.AccountingEntries.AddRange(new[]
            {
                new AccountingEntry { FinancialDocumentId = d1.Id, DebitAccountId = acc4010.Id, CreditAccountId = acc1010.Id, Amount = 185420.50m, LineNo = 1, Purpose = "Электроэнергия" },
                new AccountingEntry { FinancialDocumentId = d2.Id, DebitAccountId = acc4010.Id, CreditAccountId = acc1010.Id, Amount = 45230.00m, LineNo = 1, Purpose = "Связь" },
                new AccountingEntry { FinancialDocumentId = d3.Id, DebitAccountId = acc2010.Id, CreditAccountId = acc1010.Id, Amount = 12800.00m, LineNo = 1, Purpose = "Канцтовары" }
            });

            context.DocumentBudgetAllocations.AddRange(new[]
            {
                new DocumentBudgetAllocation { FinancialDocumentId = d1.Id, BudgetItemId = budgetItems[0].Id, Amount = 185420.50m },
                new DocumentBudgetAllocation { FinancialDocumentId = d2.Id, BudgetItemId = budgetItems[2].Id, Amount = 45230.00m },
                new DocumentBudgetAllocation { FinancialDocumentId = d3.Id, BudgetItemId = budgetItems[1].Id, Amount = 12800.00m }
            });

            context.AuditLogs.Add(new AuditLog
            {
                UserId = adminUser.Id,
                Action = "Seed",
                EntityType = "Database",
                EntityKey = "init",
                Details = "Начальное заполнение тестовыми данными"
            });

            context.AppSettings.Add(new AppSetting { Key = "App.Title", Value = "Учёт финансового отдела — Дубровский район" });
            context.SaveChanges();
        }
    }
}

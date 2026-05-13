using System;
using Lera_Diploma.Data;
using Lera_Diploma.Models;

namespace Lera_Diploma.Services
{
    public sealed class AuditService
    {
        public void Write(int? userId, string action, string entityType, string entityKey, string details)
        {
            try
            {
                using (var db = new FinancialDbContext())
                {
                    db.AuditLogs.Add(new AuditLog
                    {
                        UserId = userId,
                        Action = action ?? "",
                        EntityType = entityType,
                        EntityKey = entityKey,
                        Details = details
                    });
                    db.SaveChanges();
                }
            }
            catch
            {
                // аудит не должен ронять приложение
            }
        }
    }
}

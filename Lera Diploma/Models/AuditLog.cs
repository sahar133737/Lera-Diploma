using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        public long Id { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; }

        [Required, MaxLength(128)]
        public string Action { get; set; }

        [MaxLength(256)]
        public string EntityType { get; set; }

        [MaxLength(128)]
        public string EntityKey { get; set; }

        [MaxLength(4000)]
        public string Details { get; set; }

        public virtual User User { get; set; }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("Accounts")]
    public class Account
    {
        public int Id { get; set; }

        [Required, MaxLength(32)]
        public string Code { get; set; }

        [Required, MaxLength(512)]
        public string Name { get; set; }

        public int? ParentAccountId { get; set; }

        public virtual Account Parent { get; set; }
        public virtual ICollection<Account> Children { get; set; } = new List<Account>();

        public virtual ICollection<AccountingEntry> DebitEntries { get; set; } = new List<AccountingEntry>();
        public virtual ICollection<AccountingEntry> CreditEntries { get; set; } = new List<AccountingEntry>();
    }
}

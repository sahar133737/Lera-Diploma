using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("AccountingEntries")]
    public class AccountingEntry
    {
        public int Id { get; set; }

        public int FinancialDocumentId { get; set; }

        public int DebitAccountId { get; set; }
        public int CreditAccountId { get; set; }

        public decimal Amount { get; set; }

        public int LineNo { get; set; }

        [MaxLength(1000)]
        public string Purpose { get; set; }

        public virtual FinancialDocument FinancialDocument { get; set; }
        public virtual Account DebitAccount { get; set; }
        public virtual Account CreditAccount { get; set; }
    }
}

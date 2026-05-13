using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("FinancialDocuments")]
    public class FinancialDocument
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Number { get; set; }

        public DateTime DocumentDate { get; set; }

        public int DocumentTypeId { get; set; }
        public int DocumentStatusId { get; set; }

        public int? CounterpartyId { get; set; }

        public int ResponsibleUserId { get; set; }

        [MaxLength(2000)]
        public string Comment { get; set; }

        public virtual DocumentType DocumentType { get; set; }
        public virtual DocumentStatus DocumentStatus { get; set; }
        public virtual Counterparty Counterparty { get; set; }
        public virtual User ResponsibleUser { get; set; }

        public virtual ICollection<AccountingEntry> Entries { get; set; } = new List<AccountingEntry>();
        public virtual ICollection<DocumentBudgetAllocation> BudgetAllocations { get; set; } = new List<DocumentBudgetAllocation>();
    }
}

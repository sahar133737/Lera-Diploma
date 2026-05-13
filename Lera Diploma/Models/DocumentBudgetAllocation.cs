using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("DocumentBudgetAllocations")]
    public class DocumentBudgetAllocation
    {
        public int Id { get; set; }

        public int FinancialDocumentId { get; set; }
        public int BudgetItemId { get; set; }

        public decimal Amount { get; set; }

        public virtual FinancialDocument FinancialDocument { get; set; }
        public virtual BudgetItem BudgetItem { get; set; }
    }
}

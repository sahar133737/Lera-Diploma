using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("BudgetItems")]
    public class BudgetItem
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Code { get; set; }

        [Required, MaxLength(512)]
        public string Name { get; set; }

        public virtual ICollection<DocumentBudgetAllocation> Allocations { get; set; } = new List<DocumentBudgetAllocation>();
    }
}

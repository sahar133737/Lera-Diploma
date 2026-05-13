using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("DocumentTypes")]
    public class DocumentType
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Code { get; set; }

        [Required, MaxLength(256)]
        public string Name { get; set; }

        public virtual ICollection<FinancialDocument> Documents { get; set; } = new List<FinancialDocument>();
    }
}

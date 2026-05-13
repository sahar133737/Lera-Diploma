using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("Counterparties")]
    public class Counterparty
    {
        public int Id { get; set; }

        [Required, MaxLength(512)]
        public string Name { get; set; }

        [MaxLength(20)]
        public string Inn { get; set; }

        [MaxLength(12)]
        public string Kpp { get; set; }

        /// <summary>ЮЛ / ФЛ / ИП</summary>
        [Required, MaxLength(16)]
        public string Kind { get; set; }

        public virtual ICollection<FinancialDocument> Documents { get; set; } = new List<FinancialDocument>();
    }
}

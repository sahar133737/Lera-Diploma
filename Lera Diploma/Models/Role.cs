using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("Roles")]
    public class Role
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string Code { get; set; }

        [Required, MaxLength(256)]
        public string Name { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

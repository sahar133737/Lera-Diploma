using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        [Key, Column(Order = 0)]
        public int RoleId { get; set; }

        [Key, Column(Order = 1)]
        [Required, MaxLength(128)]
        public string PermissionKey { get; set; }

        public virtual Role Role { get; set; }
    }
}

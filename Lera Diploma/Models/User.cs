using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Login { get; set; }

        [Required, MaxLength(256)]
        public string PasswordHash { get; set; }

        [Required, MaxLength(256)]
        public string FullName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}

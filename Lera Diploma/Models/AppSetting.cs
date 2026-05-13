using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lera_Diploma.Models
{
    [Table("AppSettings")]
    public class AppSetting
    {
        [Key, MaxLength(128)]
        public string Key { get; set; }

        [MaxLength(4000)]
        public string Value { get; set; }
    }
}

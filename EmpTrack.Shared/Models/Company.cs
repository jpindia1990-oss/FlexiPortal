using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmpTrack.Shared.Models
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyCode { get; set; } = "ACECARBO";

        [Required]
        public string CompanyName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public string DatabaseName { get; set; } = string.Empty;

        public string ConnectionString { get; set; }
        public string ServerName { get; set; }

        public bool? IsGpsTrackingEnabled { get; set; } = false;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmpTrack.Shared.Models
{
    [Table("LocationLogs")]
    public class LocationLog
    {
        [Key]
        [Column("LogID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogID { get; set; }

        [Required]
        public int EmployeeID { get; set; } // FK to Employees.Id

        [Required]
        public DateTime RecordedAt { get; set; }

        public string? Address { get; set; }
        public string? MacAddress { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Longitude { get; set; }

        public int? BatteryLevel { get; set; }
        public string? DeviceOS { get; set; }
        public string CompanyCode { get; set; }
        public int? CompanyID { get; set; }
        public string E_ID { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmpTrack.Shared.Models
{
    [Table("EmployeeLocations")]
    public class EmployeeLocation
    {
        [Key]
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? UnitName { get; set; }
        public string? AreaName { get; set; }
        public string? Address { get; set; }
        public int RadiusMeters { get; set; } = 300;
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
    }
}
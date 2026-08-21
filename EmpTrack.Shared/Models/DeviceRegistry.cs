using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmpTrack.Shared.Models
{
    [Table("DeviceRegistry")]
    public class DeviceRegistry
    {
        public int Id { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? BatteryLevel { get; set; }
        public string? CompanyCode { get; set; } = "";
      
        public string? DeviceName { get; set; } = "";
        public string? DeviceOS { get; set; } = "";
        public string? EmployeeCode { get; set; } = "";
        public string DeviceID { get; set; } = "";
        public int EmployeeID { get; set; }
        public string? EmployeeName { get; set; } = "";
        public bool? IsApproved { get; set; } = false;
        public DateTime? LastSeen { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? MacAddress { get; set; } = "";
        public DateTime? RecordedAt { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public DateTime? RequestedAt { get; set; }
        public string? RequestedName { get; set; } = "";
    }
}

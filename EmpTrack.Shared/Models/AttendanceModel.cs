using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace EmpTrack.Shared.Models
{
    public class AttendanceModel
    {
        public string CompanyCode { get; set; }

        [JsonPropertyName("employeeId")]
        public string EmployeeId { get; set; } = "";
        public string EmpId { get; set; }
        public DateTime PunchTime { get; set; } = DateTime.Now;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Location { get; set; } = "";
        public string LocationReason { get; set; } = "";
    }
}

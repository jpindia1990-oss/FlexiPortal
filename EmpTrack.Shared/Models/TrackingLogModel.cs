using System;
using System.Collections.Generic;
using System.Text;

namespace EmpTrack.Shared.Models
{
    // This saves to EmpTrackDB.dbo.TrackingLogs via MobTrack API
    public class TrackingLogModel
    {
        public string DeviceId { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Address { get; set; }
    }
}
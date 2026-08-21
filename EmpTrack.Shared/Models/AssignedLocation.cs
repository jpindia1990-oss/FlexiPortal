using System;
using System.Collections.Generic;
using System.Text;

namespace EmpTrack.Shared.Models
{
    public class AssignedLocation
    {
        public int Id { get; set; }
        public int EmployeeID { get; set; } // FK to Employees.Id (PK)
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

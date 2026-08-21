namespace EmpTrack.Shared.Models
{
    public class PayrollModel
    {
        // --- Keys & Multi-tenancy ---
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = "";
        public string EmployeeId { get; set; } = "";
        public string EmployeeName { get; set; } = "";

        // --- Period ---
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthDisplay { get; set; } = ""; // e.g. "July 2025"

        // --- Attendance ---
        public int WorkingDays { get; set; }
        public int WorkedDays { get; set; }
        public int PresentDays { get; set; }
        public int LOPDays => WorkingDays - WorkedDays;

        // --- Earnings ---
        public decimal FixedGross { get; set; }
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public decimal SpecialAllowance { get; set; }
        public decimal TotalEarnings { get; set; }

        // --- Deductions ---
        public decimal PF { get; set; }
        public decimal ESI { get; set; }
        public decimal PT { get; set; }
        public decimal TotalDeductions { get; set; }

        // --- Net ---
        public decimal NetPay { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Generated";
    }
}
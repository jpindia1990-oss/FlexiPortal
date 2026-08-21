namespace EmpTrack.Shared.Models
{
    public class LoginRequest
    {
        public string CompanyCode { get; set; }
        public string EmployeeId { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace EmpTrack.Shared.Models
{
    public class LeaveBalanceModel
    {
        [JsonPropertyName("leaveType")]
        public string LeaveType { get; set; } = "";

        [JsonPropertyName("available")]
        public string Available { get; set; } = "0";

        [JsonPropertyName("enjoyed")]
        public string Enjoyed { get; set; } = "0";

        [JsonPropertyName("balance")]
        public string Balance { get; set; } = "0";
    }
}
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace FlexiPortal.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public class LeaveBalanceDto
    {
        public string Lv_Desc { get; set; }
        public double Opening { get; set; }
        public double Enjoy { get; set; }
        public double Balance { get; set; }
    }
    public class LeaveHistoryDto
    {
       
        public int Tran_Id { get; set; }
        public string e_id { get; set; }
        public DateTime Fr_date { get; set; }
        public DateTime To_date { get; set; }
        public string Lv_type { get; set; }
        public double Lv_days { get; set; }
        public string Lv_reason { get; set; }
        public string Lv_Stat { get; set; }
        public DateTime Ent_date { get; set; }
    }
    public class LeaveTypeDto
    {
        public int Lv_id { get; set; }
        public string Lv_Desc { get; set; }
        public int lvpd { get; set; }
    }

    private string GetCompanyCode()
    {
        var code = Preferences.Default.Get("CompanyCode", "");
        if (string.IsNullOrWhiteSpace(code))
            code = Preferences.Default.Get("CompCode", "");

        if (string.IsNullOrWhiteSpace(code) || code.StartsWith("P"))
        {
            // No hardcode - force re-login if code missing
            throw new Exception("CompanyCode not found. Please logout and login again.");
        }
        return code;
    }
    public async Task<List<LeaveBalanceDto>> GetLeaveBalanceAsync(string empId)
    {
        string companyCode = GetCompanyCode();
        var resp = await _httpClient.GetAsync($"api/Leave/Balance/{empId}?companyCode={companyCode}");
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) throw new Exception(json);
        return JsonSerializer.Deserialize<List<LeaveBalanceDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<List<LeaveHistoryDto>> GetLeaveHistoryAsync(string empId)
    {
        string companyCode = GetCompanyCode();
        var resp = await _httpClient.GetAsync($"api/Leave/History/{empId}?companyCode={companyCode}");
        var json = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json) || json.TrimStart().StartsWith("<")) return new List<LeaveHistoryDto>();
        return JsonSerializer.Deserialize<List<LeaveHistoryDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync()
    {
        string companyCode = GetCompanyCode();
        var resp = await _httpClient.GetAsync($"api/Leave/Types?companyCode={companyCode}");
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<LeaveTypeDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<double> GetWorkingDaysAsync(string empId, DateTime fromDate, DateTime toDate)
    {
        string companyCode = GetCompanyCode();
        string fr = fromDate.ToString("dd/MM/yyyy");
        string to = toDate.ToString("dd/MM/yyyy");
        var resp = await _httpClient.GetAsync($"api/Leave/WorkingDays?empId={empId}&fromDate={fr}&toDate={to}&companyCode={companyCode}");
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return (toDate - fromDate).Days + 1;
        try
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("days").GetDouble();
        }
        catch { return (toDate - fromDate).Days + 1; }
    }

    public async Task<bool> SubmitLeaveEntryAsync(string empId, string fr, string to, string lvType, double lvDays, string Tran_Id, string tran_id1, string reason)
    {
        string companyCode = GetCompanyCode();
        if (string.IsNullOrWhiteSpace(Tran_Id)) Tran_Id = "New";
        string lvId = lvType == "EL" ? "1" : lvType == "CL" ? "2" : lvType == "SL" ? "3" : lvType == "CO" ? "4" : "1";
        var payload = new { e_id = empId, Fr_date = fr, To_date = to, Lv_type = lvType, Lv_days = lvDays, Lv_id = lvId, Tran_Id = Tran_Id, tran_id1 = tran_id1, Lv_reason = reason };
        var response = await _httpClient.PostAsJsonAsync($"api/Leave/Entry?companyCode={companyCode}", payload);
        var txt = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new Exception(txt);
        return true;
    }

    public async Task<bool> SaveAttendanceAsync(object data)
    {
        string companyCode = GetCompanyCode()?.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(companyCode))
            throw new Exception("CompanyCode empty - login again");

        // Convert data to Dictionary and force CompanyCode inside body
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                   ?? new Dictionary<string, object>();

        dict["CompanyCode"] = companyCode;
        dict["companyCode"] = companyCode;
        dict["Company"] = companyCode;

        if (!dict.ContainsKey("EmpId") && dict.ContainsKey("EmployeeId"))
            dict["EmpId"] = dict["EmployeeId"];

        // THIS IS THE FIX - Your API is [HttpPost("{companyCode}")] NOT "Save"
        var resp = await _httpClient.PostAsJsonAsync($"api/Attendance/{companyCode}", dict);

        var txt = await resp.Content.ReadAsStringAsync();

        System.Diagnostics.Debug.WriteLine($"PUNCH RESP: {resp.StatusCode} - {txt}");

        if (!resp.IsSuccessStatusCode) throw new Exception(txt);
        return true;
    }

    public async Task<List<PendingLeaveDto>> GetAllLeavesAsync(string mgrId)
    {
        try
        {
            string companyCode = GetCompanyCode();
            var resp = await _httpClient.GetAsync($"api/Leave/All/{mgrId}?companyCode={companyCode}");
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return new List<PendingLeaveDto>();
            return JsonSerializer.Deserialize<List<PendingLeaveDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new List<PendingLeaveDto>(); }
    }

    public async Task<bool> ApproveLeaveAsync(int tranId, string managerId, string status)
    {
        try
        {
            string companyCode = GetCompanyCode();
            var payload = new { TranId = tranId, ManagerId = managerId, Status = status };
            var response = await _httpClient.PostAsJsonAsync($"api/Leave/Approve?companyCode={companyCode}", payload);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

 

    public async Task<List<PendingLeaveDto>> GetApprovedLeavesAsync(string managerId)
    {
        try
        {
            string companyCode = GetCompanyCode();
            var resp = await _httpClient.GetAsync($"api/Leave/Approved/{managerId}?companyCode={companyCode}");
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return new List<PendingLeaveDto>();
            return JsonSerializer.Deserialize<List<PendingLeaveDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new List<PendingLeaveDto>(); }
    }

    public async Task<List<PendingLeaveDto>> GetRejectedLeavesAsync(string managerId)
    {
        try
        {
            string companyCode = GetCompanyCode();
            var resp = await _httpClient.GetAsync($"api/Leave/Rejected/{managerId}?companyCode={companyCode}");
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return new List<PendingLeaveDto>();
            return JsonSerializer.Deserialize<List<PendingLeaveDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new List<PendingLeaveDto>(); }
    }
    public async Task<(bool success, string message)> DeleteLeaveAsyncWithMessage(int tranId)
    {
        string c = GetCompanyCode();
        var resp = await _httpClient.DeleteAsync($"api/Leave/Delete/{tranId}?companyCode={c}");
        var txt = await resp.Content.ReadAsStringAsync();
        return (resp.IsSuccessStatusCode, txt);
    }



    public async Task<bool> DeleteLeaveAsync(int tranId)
    {
        try
        {
            string c = GetCompanyCode();
            var resp = await _httpClient.DeleteAsync($"api/Leave/Delete/{tranId}?companyCode={c}");
            var txt = await resp.Content.ReadAsStringAsync();
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
    public async Task<List<PendingLeaveDto>> GetPendingLeavesAsync(string managerId)
    {
        try
        {
            string companyCode = GetCompanyCode();
            var resp = await _httpClient.GetAsync($"api/Leave/Pending/{managerId}?companyCode={companyCode}");
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return new List<PendingLeaveDto>();
            return JsonSerializer.Deserialize<List<PendingLeaveDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new List<PendingLeaveDto>(); }
    }
    public class PendingLeaveDto
    {
        public int Tran_Id { get; set; }
        public string E_ID { get; set; }
        public string E_Name { get; set; }
        public string Lv_type { get; set; }
        public string Fr_date { get; set; }
        public string To_date { get; set; }
        public double t_dys { get; set; }
        public string Lv_reason { get; set; }
        public string lv_stat { get; set; }
        public string aply_dt { get; set; }


    }

}
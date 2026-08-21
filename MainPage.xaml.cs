using System.Net.Http.Json;
using FlexiPortal.Mobile.Helpers;
namespace FlexiPortal.Mobile.Pages;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _client;
    public MainPage(HttpClient client) { InitializeComponent(); _client = client; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadUser();
        await CheckDeviceApproval();
        await CheckTodayPunch();
        LoadMiniCalendar();
        await LoadHolidays();

    }

    private void LoadUser()
    {
        var empName = Preferences.Default.Get("EmployeeName", "Employee");
        var empId = Preferences.Default.Get("EmployeeId", "");
        var company = Preferences.Default.Get("CompanyCode", "");
        if (EmployeeNameLabel != null) EmployeeNameLabel.Text = $"Welcome, {empName}";
        if (EmployeeIdLabel != null) EmployeeIdLabel.Text = $"ID: {empId} | {company}";
        if (InitialLabel != null && empName.Length > 0) InitialLabel.Text = empName.Substring(0, 1).ToUpper();
    }

    private string GetDeviceId()
    {
        string id = Preferences.Default.Get("DeviceUUID", "");
        if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString().ToUpperInvariant();
        Preferences.Default.Set("DeviceUUID", id);
        return id;
    }

    private async Task CheckDeviceApproval()
    {
        try
        {
            bool isGpsEnabled = Preferences.Default.Get("IsGpsTrackingEnabled", false);
            bool isMobilePunch = Preferences.Default.Get("IsMobilePunch", true);
            bool isApproved = Preferences.Default.Get("IsDeviceApproved", true);
            string deviceStatus = Preferences.Default.Get("DeviceStatus", "");

            // FORCE DEBUG ON SCREEN
            ApprovalStatusLabel.Text = $"Punch={isMobilePunch} GPS={isGpsEnabled} Approved={isApproved} Status={deviceStatus}";
            System.Diagnostics.Debug.WriteLine($"MAIN: {ApprovalStatusLabel.Text}");

            // === CASE 1: IsMobilePunch = 0 ===
            if (!isMobilePunch)
            {
                ApprovalStatusLabel.Text = "❌";
                ApprovalStatusLabel.TextColor = Colors.Red;
                StatusBorder.BackgroundColor = Color.FromArgb("#FEE2E2");
                DeviceApprovalCard.IsVisible = false;
                if (PunchCard != null) { PunchCard.IsVisible = false; PunchCard.IsEnabled = false; }
                MenuGrid.Opacity = 0.6;
                return;
            }

            // CASE 2: GPS Not Required
            if (!isGpsEnabled)
            {
                ApprovalStatusLabel.Text = "✅";
                ApprovalStatusLabel.TextColor = Colors.Green;
                StatusBorder.BackgroundColor = Color.FromArgb("#DCFCE7");
                DeviceApprovalCard.IsVisible = false;
                if (PunchCard != null) { PunchCard.IsVisible = true; PunchCard.IsEnabled = true; }
                MenuGrid.IsEnabled = true; MenuGrid.Opacity = 1;
                return;
            }

            // CASE 3: GPS Required + Approved
            if (isApproved && deviceStatus == "Approved")
            {
                ApprovalStatusLabel.Text = "✅ Approved";
                ApprovalStatusLabel.TextColor = Colors.Green;
                StatusBorder.BackgroundColor = Color.FromArgb("#DCFCE7");
                DeviceApprovalCard.IsVisible = false;
                if (PunchCard != null) { PunchCard.IsVisible = true; PunchCard.IsEnabled = true; }
                return;
            }

            // CASE 4: Pending
            ApprovalStatusLabel.Text = "⏳ Pending Approval";
            ApprovalStatusLabel.TextColor = Color.FromArgb("#92400E");
            StatusBorder.BackgroundColor = Color.FromArgb("#FEF3C7");
            DeviceApprovalCard.IsVisible = true;
            RequestApprovalBtn.IsVisible = deviceStatus == "NotRegistered";
            if (PunchCard != null) { PunchCard.IsVisible = false; }
        }
        catch (Exception ex) { ApprovalStatusLabel.Text = ex.Message; }
        finally { LoadingIndicator.IsVisible = false; }
    }

    private void SetMenuViewOnly()
    {
        if (PunchCard != null) { PunchCard.IsVisible = false; PunchCard.IsEnabled = false; }
    }

    private async Task CheckTodayPunch()
    {
        try
        {
            var empId = Preferences.Default.Get("EmployeeId", "");
            var company = Preferences.Default.Get("CompanyCode", "");
            if (string.IsNullOrWhiteSpace(empId)) return;
            var todayData = await _client.GetFromJsonAsync<TodayResponse>($"api/Attendance/Today/{company}/{empId}");
            string text;
            if (todayData != null && todayData.punches != null && todayData.punches.Count > 0)
            {
                if (todayData.punches.Count == 1)
                    text = $"Punched 1 time at {todayData.punches[0].time:hh:mm tt}";
                else
                    text = $"Punched {todayData.count} times - {string.Join(", ", todayData.punches.Select(p => $"{p.type}: {p.time:hh:mm tt}"))}";
            }
            else text = "Not Punched Today";

            PunchStatusLabelTop.Text = text;
            PunchStatusLabelTop.Text = text;
        }
        catch { PunchStatusLabelTop.Text = "Not Punched Today"; PunchStatusLabelTop.Text = "Not Punched Today"; }
    }

    public class TodayResponse
    {
        public bool punchedIn { get; set; }
        public int count { get; set; }
        public List<PunchDetail> punches { get; set; } = new();
    }
    public class PunchDetail { public DateTime time { get; set; } public string type { get; set; } = ""; }

    private async void OnPunchPageClicked(object sender, EventArgs e)
    {
        bool isPunch = Preferences.Default.Get("IsMobilePunch", false);
        
        bool isGps = Preferences.Default.Get("IsGpsTrackingEnabled", false);
        bool isApproved = Preferences.Default.Get("IsDeviceApproved", false);
        if (isGps && !isApproved)
        {
            await DisplayAlert("Blocked", "Device not approved", "OK");
            return;
        }
        await Shell.Current.GoToAsync("//AttendancePage");
    }

    private async void OnAttendancePageClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//AttendanceHistoryPage");
    private async void OnPayslipPageClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//PayrollPage");
    private async void OnLeavePageClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//LeaveBalancePage");

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Logout?", "Yes", "No");
        if (!confirm) return;

        string savedCompany = Preferences.Default.Get("SavedCompanyCode", "");
        string savedEmp = Preferences.Default.Get("SavedEmployeeId", "");
        bool remember = Preferences.Default.Get("RememberMe", false);
        string deviceId = Preferences.Default.Get("DeviceUUID", "");

        Preferences.Default.Clear();

        if (!string.IsNullOrEmpty(deviceId)) Preferences.Default.Set("DeviceUUID", deviceId);
        if (remember)
        {
            Preferences.Default.Set("SavedCompanyCode", savedCompany);
            Preferences.Default.Set("SavedEmployeeId", savedEmp);
            Preferences.Default.Set("RememberMe", true);
        }

        var loginPage = IPlatformApplication.Current.Services.GetRequiredService<LoginPage>();
        Application.Current.MainPage = new NavigationPage(loginPage);
    }

  
    private async void OnRequestApprovalClicked(object sender, EventArgs e)
    {
        try
        {
            RequestApprovalBtn.IsEnabled = false;
            var deviceId = GetDeviceId();
            var payload = new { CompanyCode = Preferences.Default.Get("CompanyCode", ""), EmployeeId = Preferences.Default.Get("EmployeeId", ""), MacAddress = deviceId, DeviceName = DeviceInfo.Current.Model };
            var res = await _client.PostAsJsonAsync($"api/DeviceRegistry/request", payload);
            if (res.IsSuccessStatusCode) await DisplayAlert("Success", "Request sent", "OK");
            else await DisplayAlert("Error", await res.Content.ReadAsStringAsync(), "OK");
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        finally { RequestApprovalBtn.IsEnabled = true; }
    }

    public class DeviceStatusDto { public bool exists { get; set; } public bool isApproved { get; set; } }


    private void LoadMiniCalendar()
    {
        try
        {
            var now = DateTime.Now;
            if (MonthYearLabel != null)
                MonthYearLabel.Text = now.ToString("MMM yyyy");

            var firstDay = new DateTime(now.Year, now.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            int startDayOfWeek = (int)firstDay.DayOfWeek;

            var dates = new List<CalendarDay>();
            for (int i = 0; i < startDayOfWeek; i++)
                dates.Add(new CalendarDay { Day = "", Bg = Colors.Transparent, Color = Colors.Transparent });

            for (int d = 1; d <= daysInMonth; d++)
            {
                bool isToday = d == now.Day;
                dates.Add(new CalendarDay
                {
                    Day = d.ToString(),
                    Bg = isToday ? Color.FromArgb("#2563eb") : Colors.Transparent,
                    Color = isToday ? Colors.White : Colors.Black
                });
            }
            CalendarView.ItemsSource = dates;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Calendar error: {ex.Message}");
        }
    }

    public class CalendarDay
    {
        public string Day { get; set; } = "";
        public Color Bg { get; set; } = Colors.Transparent;
        public Color Color { get; set; } = Colors.Black;
    }


    public class HolidayDto
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string DateString => Date.ToString("dd MMM yyyy");
        public string Day => Date.Day.ToString();
        public string Month => Date.ToString("MMM").ToUpper();
    }

    private async Task LoadHolidays()
    {
        try
        {
            var company = Preferences.Default.Get("CompanyCode", "");
            var empId = Preferences.Default.Get("EmployeeId", "");

            // YOUR FINAL ROUTE
            var url = $"api/Attendance/Holidays/Upcoming/{company}/{empId}";
            System.Diagnostics.Debug.WriteLine($"Loading holidays from {url}");

            var holidays = await _client.GetFromJsonAsync<List<HolidayDto>>(url);

            if (holidays != null && holidays.Any())
            {
                HolidayCollection.ItemsSource = holidays.OrderBy(x => x.Date).Take(5).ToList();
            }
            else
            {
                HolidayCollection.ItemsSource = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Holiday Error: {ex.Message}");
        }

    }
    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        // Step 1: Old Password
        string oldPwd = await DisplayPromptAsync("Change Password", "Enter Old Password", "Next", "Cancel", placeholder: "Old Password", maxLength: 20, keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(oldPwd)) return;

        // Step 2: New Password
        string newPwd = await DisplayPromptAsync("Change Password", "Enter New Password (Min 8 chars | 1 Upper | 1 Number | 1 Special)", "Next", "Cancel", placeholder: "New Password", maxLength: 20, keyboard: Keyboard.Text);
        if (string.IsNullOrWhiteSpace(newPwd)) return;

        if (newPwd.Trim().Length < 8)
        {
            await DisplayAlert("Error", "Password must be at least 8 characters", "OK");
            return;
        }

        // Step 3: Confirm
        string confirmPwd = await DisplayPromptAsync("Change Password", "Confirm New Password", "Update", "Cancel", placeholder: "Confirm Password", maxLength: 20, keyboard: Keyboard.Text);
        if (newPwd.Trim() != confirmPwd.Trim())
        {
            await DisplayAlert("Error", "New password and confirm not matching", "OK");
            return;
        }

        try
        {
            var company = Preferences.Default.Get("CompanyCode", "");
            var empId = Preferences.Default.Get("EmployeeId", "");

            var payload = new
            {
                OldPassword = oldPwd.Trim(),
                NewPassword = newPwd.Trim()
            };

            var response = await _client.PostAsJsonAsync($"api/Attendance/ChangePassword/{company}/{empId}", payload);
            var resultText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Success", "Password changed successfully!", "OK");
            }
            else
            {
                await DisplayAlert("Failed", resultText, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
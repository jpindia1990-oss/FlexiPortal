
using FlexiPortal.Mobile.Services;
using System.Text;
using System.Text.Json;
using System.Diagnostics;


namespace FlexiPortal.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly HttpClient _client;
    private readonly LocationService _locationService;

    public LoginPage(HttpClient client, LocationService locationService)
    {
        InitializeComponent();
        _client = client;
        _locationService = locationService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CompanyCodeEntry.Text = Preferences.Default.Get("SavedCompanyCode", "");
        EmpIdEntry.Text = Preferences.Default.Get("SavedEmployeeId", "");
        RememberCheckBox.IsChecked = Preferences.Default.Get("RememberMe", true);
        RequestApprovalBtn.IsVisible = false;
        InfoLabel.IsVisible = false;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            StatusLabel.Text = "Checking...";
            StatusDot.Color = Colors.Orange;
            RequestApprovalBtn.IsVisible = false;

            var companyCode = CompanyCodeEntry.Text?.Trim()?.ToUpper();
            var empId = EmpIdEntry.Text?.Trim()?.ToUpper();
            var password = PassEntry.Text?.Trim();

            if (string.IsNullOrEmpty(companyCode) || string.IsNullOrEmpty(empId) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Enter Company, Employee ID and Password", "OK");
                StatusLabel.Text = "Ready"; StatusDot.Color = Colors.Red; return;
            }

            string deviceUUID = await GetOrCreateDeviceId();
            string realDeviceName = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();
            string realDeviceOS = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}".Trim();

            var loginData = new
            {
                CompanyCode = companyCode,
                Username = empId,
                EmployeeId = empId,
                Password = password,
                DeviceId = deviceUUID,
                MacAddress = deviceUUID,
                DeviceName = realDeviceName,
                DeviceOS = realDeviceOS,
                DeviceModel = DeviceInfo.Current.Model
            };

            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/MobileAuth/login", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                StatusLabel.Text = "Failed"; StatusDot.Color = Colors.Red;
                await DisplayAlert("Login Failed", responseString, "OK");
                return;
            }

            var result = JsonSerializer.Deserialize<LoginResponse>(responseString,
              new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || result.success == false)
            {
                await DisplayAlert("Failed", result?.message ?? responseString, "OK");
                StatusLabel.Text = "Failed"; StatusDot.Color = Colors.Red; return;
            }

            string finalEmpId = (result.employeeId ?? empId).Trim().ToUpper();
            string finalName = result.employeeName ?? result.name ?? finalEmpId;

            Preferences.Default.Set("Token", result.token ?? "");
            Preferences.Default.Set("EmployeeId", finalEmpId);
            Preferences.Default.Set("CompanyCode", companyCode);
            Preferences.Default.Set("EmployeeName", finalName);
            Preferences.Default.Set("IsLoggedIn", true);
            Preferences.Default.Set("IsGpsTrackingEnabled", result.isGpsTrackingEnabled);
            Preferences.Default.Set("IsMobilePunch", result.isMobilePunch);
            Preferences.Default.Set("IsDeviceApproved", result.isDeviceApproved);
            Preferences.Default.Set("DeviceStatus", result.deviceStatus ?? "");
            Preferences.Default.Set("DeviceUUID", deviceUUID);
            Preferences.Default.Set("TransactionID", result.transactionId);
           

            if (RememberCheckBox?.IsChecked == true)
            {
                Preferences.Default.Set("SavedCompanyCode", companyCode);
                Preferences.Default.Set("SavedEmployeeId", finalEmpId);
                Preferences.Default.Set("RememberMe", true);
            }
            else
            {
                Preferences.Default.Remove("SavedCompanyCode");
                Preferences.Default.Remove("SavedEmployeeId");
                Preferences.Default.Set("RememberMe", false);
            }

            // === GPS LOGIC - NO AUTO REQUEST ===
            if (result.isGpsTrackingEnabled && !result.isDeviceApproved)
            {
                StatusLabel.Text = result.deviceStatus == "NotRegistered" ? "Device Not Registered" : "Pending Approval";
                StatusDot.Color = Colors.Orange;
                RequestApprovalBtn.IsVisible = true;
                InfoLabel.IsVisible = true;
                InfoLabel.Text = result.deviceStatus == "NotRegistered"
                    ? $"Device '{realDeviceName}' needs approval. Click Above."
                    : $"Device '{realDeviceName}' pending admin approval.";

                await DisplayAlert("Approval Required",
                    result.deviceStatus == "NotRegistered"
                    ? "Click REQUEST DEVICE APPROVAL to send request to admin."
                    : "Your device is pending. Contact admin.", "OK");
                return; // DO NOT GO TO AppShell
            }

            StatusLabel.Text = "Success"; StatusDot.Color = Colors.Green;

         
            int numericId = result.employeeNumericId ?? result.employeeIdInt ?? result.transactionId;
            Preferences.Default.Set("EmployeeNumericId", numericId);
            Preferences.Default.Set("E_ID", numericId);
            Preferences.Default.Set("DeviceMac", deviceUUID);
            Preferences.Default.Set("DeviceUUID", deviceUUID);
            Preferences.Default.Set("CompanyID", result.companyId ?? 1);
            Preferences.Default.Set("CompanyCode", companyCode);
            Preferences.Default.Set("IsLoggedIn", true);

            // Send ONE login log only - DO NOT start timer here
            try
            {
                var loc = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                if (loc != null)
                    await _locationService.SendLocationAsync(loc.Latitude, loc.Longitude, "Login-First");
            }
            catch (Exception ex) { Debug.WriteLine($"Login log failed: {ex.Message}"); }

            // Go to shell - App.xaml.cs OnStart will start the 2-min timer
            Application.Current.MainPage = new AppShell();






        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Error"; StatusDot.Color = Colors.Red;
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnRequestApprovalClicked(object sender, EventArgs e)
    {
        try
        {
            RequestApprovalBtn.IsEnabled = false;
            StatusLabel.Text = "Sending...";

            var companyCode = CompanyCodeEntry.Text?.Trim()?.ToUpper();
            var empId = EmpIdEntry.Text?.Trim()?.ToUpper();
            string deviceUUID = await GetOrCreateDeviceId();
            string realDeviceName = $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}".Trim();

            var reqData = new
            {
                CompanyCode = companyCode,
                EmployeeId = empId,
                Username = empId,
                DeviceId = deviceUUID,
                MacAddress = deviceUUID,
                DeviceName = realDeviceName,
                DeviceOS = $"{DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}",
                DeviceModel = DeviceInfo.Current.Model
            };

            var json = JsonSerializer.Serialize(reqData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("api/MobileAuth/request-approval", content);
            var respStr = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Sent", "Request sent to admin.", "OK");
                StatusLabel.Text = "Request Sent - Wait for approval";
                RequestApprovalBtn.IsVisible = false;
            }
            else
            {
                await DisplayAlert("Failed", respStr, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally { RequestApprovalBtn.IsEnabled = true; }
    }

    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        PassEntry.IsPassword = !PassEntry.IsPassword;
        ToggleBtn.Text = PassEntry.IsPassword ? "Show" : "Hide";
    }

    private async Task<string> GetOrCreateDeviceId()
    {
        string id = Preferences.Default.Get("DeviceUUID", "");
        if (!string.IsNullOrEmpty(id)) return id.ToUpperInvariant();
        id = Guid.NewGuid().ToString().ToUpperInvariant();
        Preferences.Default.Set("DeviceUUID", id);
        Preferences.Default.Set("DeviceMac", id);
        return id;
    }

    public class LoginResponse
    {
        public bool success { get; set; }
        public string message { get; set; } = "";
        public string token { get; set; } = "";
        public string employeeId { get; set; } = "";
        public string employeeName { get; set; } = "";
        public string name { get; set; } = "";
        public bool isGpsTrackingEnabled { get; set; }
        public bool isMobilePunch { get; set; }
        public bool isDeviceApproved { get; set; }
        public string deviceStatus { get; set; } = "";
        public int transactionId { get; set; }
        public int? employeeNumericId { get; set; } // ADD THIS
        public int? employeeIdInt { get; set; } // ADD THIS
        public int? companyId { get; set; } 
    }
}
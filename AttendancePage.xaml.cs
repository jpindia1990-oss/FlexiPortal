using EmpTrack.Shared.Models;
using FlexiPortal.Mobile.Services;
using Microsoft.Maui.Storage;

namespace FlexiPortal.Mobile.Pages;

public partial class AttendancePage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly LocationService _locationService;
    private bool _isPunchedIn = false;
    private bool _isBlocked = false;

    public AttendancePage(ApiService apiService, LocationService locationService)
    {
        InitializeComponent();
        _apiService = apiService;
        _locationService = locationService;
    }

    // === THIS IS THE FIX - BLOCK PAGE ITSELF ===
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool isPunch = Preferences.Default.Get("IsMobilePunch", false);
        bool isGps = Preferences.Default.Get("IsGpsTrackingEnabled", false);
        bool isApproved = Preferences.Default.Get("IsDeviceApproved", !isGps);

        if (!isPunch)
        {
            _isBlocked = true;
            await DisplayAlert("Blocked", "Mobile Punch is disabled for you (IsMobilePunch=0). Contact HR.", "OK");
            await Shell.Current.GoToAsync("//DashboardPage");
            return;
        }
        if (isGps && !isApproved)
        {
            _isBlocked = true;
            await DisplayAlert("Blocked", "Device not approved. Contact HR.", "OK");
            await Shell.Current.GoToAsync("//DashboardPage");
            return;
        }
        _isBlocked = false;
    }

    private async void OnPunchInClicked(object sender, EventArgs e)
    {
        if (_isBlocked) return;

        if (!Preferences.Default.Get("IsMobilePunch", false))
        {
            await DisplayAlert("Blocked", "Mobile Punch is disabled", "OK");
            await Shell.Current.GoToAsync("//DashboardPage");
            return;
        }

        string empId = Preferences.Default.Get("EmployeeId", "")?.Trim().ToUpper();
        if (string.IsNullOrEmpty(empId)) empId = Preferences.Default.Get("LoggedInEmployeeId", "")?.Trim().ToUpper();
        string companyCode = Preferences.Default.Get("CompanyCode", "")?.Trim().ToUpper();

        if (string.IsNullOrEmpty(empId) || string.IsNullOrEmpty(companyCode))
        {
            await DisplayAlert("Login First", "Please login again.", "OK");
            return;
        }

        // FIX: Declare at top so they exist everywhere
        string fullAddress = "";
        string reason = "Head Office";
        int numericId = Preferences.Default.Get("TransactionID", 0);
        double lat = -13.962612;
        double lon = 33.774119;

        try
        {
            PunchBtn.IsEnabled = false;
            StatusLabel.Text = "Getting GPS...";

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15)));

            if (location != null)
            {
                lat = location.Latitude;
                lon = location.Longitude;
            }

            if (!string.IsNullOrWhiteSpace(LocationReasonEntry.Text))
                reason = LocationReasonEntry.Text.Trim();

            StatusLabel.Text = "Getting Address...";
            fullAddress = await _locationService.GetAddressFromLatLong(lat, lon);
            if (string.IsNullOrWhiteSpace(fullAddress))
                fullAddress = $"{lat},{lon}";

            StatusLabel.Text = $"Address: {fullAddress}";

            var attendance = new AttendanceModel
            {
                CompanyCode = companyCode,
                EmployeeId = empId,
                EmpId = empId,
                PunchTime = DateTime.Now,
                Latitude = lat,
                Longitude = lon,
                Location = fullAddress,
                LocationReason = reason
            };

            StatusLabel.Text = $"Saving to Flexi_{companyCode}...";
            bool savedToHr = await _apiService.SaveAttendanceAsync(attendance);

            if (savedToHr)
            {
                // FIX: Correct order - 5 params
                await _locationService.SendLocationAsync(lat, lon, fullAddress, reason, numericId);

                PunchBtn.BackgroundColor = _isPunchedIn ? Color.FromArgb("#22c55e") : Color.FromArgb("#ef4444");
                _isPunchedIn = !_isPunchedIn;
                StatusLabel.Text = $"Punched | {fullAddress} | {DateTime.Now:hh:mm tt}";
            }
            else
            {
                StatusLabel.Text = "Failed to save";
                await DisplayAlert("Failed", $"Could not save to Flexi_{companyCode}.", "OK");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            PunchBtn.IsEnabled = true;
        }
    }
}
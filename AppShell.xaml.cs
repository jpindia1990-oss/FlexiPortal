namespace FlexiPortal.Mobile;

using FlexiPortal.Mobile.Pages;
using FlexiPortal.Mobile.Services;
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        MessagingCenter.Subscribe<object>(this, "UpdateMenu", (s) => UpdateMenuVisibility());
        Routing.RegisterRoute(nameof(AttendanceHistoryPage), typeof(AttendanceHistoryPage));
        Routing.RegisterRoute(nameof(LeaveApprovalPage), typeof(Pages.LeaveApprovalPage));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateMenuVisibility();
        try
        {
            if (Preferences.Default.Get("IsLoggedIn", false) && Preferences.Default.Get("IsDeviceApproved", false))
            {
                var locService = IPlatformApplication.Current.Services.GetService<FlexiPortal.Mobile.Services.LocationService>();
                locService?.StartAutoTracking();
                System.Diagnostics.Debug.WriteLine("[AppShell] AutoTracking Started - 2 min");
            }
        }
        catch { }
    }

    public void UpdateMenuVisibility()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool isPunch = Preferences.Default.Get("IsMobilePunch", false);
            bool isGps = Preferences.Default.Get("IsGpsTrackingEnabled", false);
            bool isApproved = Preferences.Default.Get("IsDeviceApproved", !isGps);
            bool showPunch = isPunch && (!isGps || isApproved);

            if (PunchFlyout != null)
            {
                PunchFlyout.IsVisible = showPunch;
                PunchFlyout.IsEnabled = showPunch;
            }
        });
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);
        if (args.Target.Location.OriginalString.Contains("AttendancePage", StringComparison.OrdinalIgnoreCase))
        {
            bool isPunch = Preferences.Default.Get("IsMobilePunch", false);
            bool isGps = Preferences.Default.Get("IsGpsTrackingEnabled", false);
            bool isApproved = Preferences.Default.Get("IsDeviceApproved", !isGps);
            if (!isPunch || (isGps && !isApproved))
            {
                args.Cancel();
                Dispatcher.Dispatch(async () =>
                {
                    await DisplayAlert("Blocked", !isPunch ? "Mobile Punch disabled" : "Device pending approval", "OK");
                    await GoToAsync("//MainPage");
                });
            }
        }
    }
}
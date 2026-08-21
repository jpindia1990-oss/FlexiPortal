using FlexiPortal.Mobile.Services;

namespace FlexiPortal.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = Preferences.Default.Get("IsLoggedIn", false) ? new AppShell() : new Pages.LoginPage(
            IPlatformApplication.Current.Services.GetService<HttpClient>(),
            IPlatformApplication.Current.Services.GetService<LocationService>()
        );
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Created += (s, e) => StartTracking();
        window.Resumed += (s, e) => StartTracking();
        return window;
    }

    protected override void OnStart() => StartTracking();
    protected override void OnResume() => StartTracking();

    void StartTracking()
    {
        try
        {
            if (!Preferences.Default.Get("IsLoggedIn", false)) return;
            if (!Preferences.Default.Get("IsDeviceApproved", false)) return;
            if (Preferences.Default.Get("EmployeeNumericId", 0) == 0 && Preferences.Default.Get("E_ID", 0) == 0) return;

            var locService = IPlatformApplication.Current.Services.GetService<LocationService>();
            locService?.StartAutoTracking(); // has _isRunning guard
        }
        catch { }
    }
}
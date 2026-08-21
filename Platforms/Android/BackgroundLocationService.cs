#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using FlexiPortal.Mobile.Services;
using EmpTrack.Shared.Models;


namespace FlexiPortal.Mobile.Platforms.Android;

[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class BackgroundLocationService : Service
{
    private LocationService? _locationService;
    private PowerManager.WakeLock? _wakeLock;
    private System.Timers.Timer? _timer;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (_timer != null) return StartCommandResult.Sticky;

        CreateNotificationChannel();
        var notification = new NotificationCompat.Builder(this, "location_tracking")
            .SetContentTitle("FlexiPortal Running")
            .SetContentText("Sending GPS to EmpTrackDB every 12 min + Attendance to Flexi_ACECARBO")
            .SetSmallIcon(global::Android.Resource.Drawable.SymDefAppIcon)
            .SetOngoing(true)
            .Build();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            StartForeground(100, notification, global::Android.Content.PM.ForegroundService.TypeLocation);
        else
            StartForeground(100, notification);

        var powerManager = (PowerManager)GetSystemService(PowerService)!;
        _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "FlexiPortal::LocationWakeLock12Min");
        _wakeLock.Acquire();

        _locationService = IPlatformApplication.Current?.Services.GetService<LocationService>();

        _timer = new System.Timers.Timer(12 * 60 * 1000); // 12 minutes
        _timer.Elapsed += async (s, e) => await GetAndSendLocation();
        _timer.AutoReset = true;
        _timer.Enabled = true;

        _ = GetAndSendLocation();

        return StartCommandResult.Sticky;
    }

    private async Task GetAndSendLocation()
    {
        try
        {
            global::Android.Util.Log.Info("FlexiPortal", $"[TICK] {DateTime.Now:HH:mm:ss}");

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15)));

            // FIXED: is not null + 3 params (reason)
            if (location is not null && _locationService is not null)
            {
                await _locationService.SendLocationAsync(location.Latitude, location.Longitude, "Background-12Min");
                global::Android.Util.Log.Info("FlexiPortal", $"[SEND EmpTrackDB] {location.Latitude},{location.Longitude}");
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("FlexiPortal", $"ERROR: {ex.Message}");
        }
    }

    public override void OnDestroy()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        if (_wakeLock?.IsHeld == true)
            _wakeLock.Release();
        base.OnDestroy();
    }

    void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel("location_tracking", "Location Tracking", NotificationImportance.Low);
            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            manager.CreateNotificationChannel(channel);
        }
    }
}
#endif
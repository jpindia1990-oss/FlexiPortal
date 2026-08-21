#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using FlexiPortal.Mobile.Helpers;
using FlexiPortal.Mobile;
using System.Globalization;
using System.Net.Http.Json;


namespace FlexiPortal.Mobile.Platforms.Android
{
    [Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationForegroundService : Service
    {
        System.Threading.Timer _timer;
        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            var channelId = "location_channel";
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Location Tracking", NotificationImportance.Low);
                (GetSystemService(NotificationService) as NotificationManager)?.CreateNotificationChannel(channel);
            }

            var notification = new NotificationCompat.Builder(this, channelId)
         .SetContentTitle("FlexiPortal Tracking")
         .SetContentText("Sending location every 12 min")
         .SetSmallIcon(global::Android.Resource.Drawable.SymDefAppIcon)
         .SetOngoing(true)
         .Build();

            StartForeground(1001, notification);

            _timer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    var loc = await Microsoft.Maui.Devices.Sensors.Geolocation.Default.GetLastKnownLocationAsync()
                            ?? await Microsoft.Maui.Devices.Sensors.Geolocation.Default.GetLocationAsync(
                                  new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

                    if (loc != null)
                    {
                        var mac = Preferences.Default.Get("DeviceUUID", "");
                        var companyCode = Preferences.Default.Get("CompanyCode", "ACECARBO");
                        var empCode = Preferences.Default.Get("EmployeeId", "P0007");
                        string address = "";

                        // TRY NOMINATIM WITH LOG
                        try
                        {
                            using var httpAddr = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                            httpAddr.DefaultRequestHeaders.Add("User-Agent", "FlexiPortalApp/1.0");
                            var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={loc.Latitude.ToString(CultureInfo.InvariantCulture)}&lon={loc.Longitude.ToString(CultureInfo.InvariantCulture)}&zoom=18&addressdetails=1";
                            var resp = await httpAddr.GetAsync(url);
                            var json = await resp.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"[Service] Nominatim {resp.StatusCode}: {json.Substring(0, Math.Min(200, json.Length))}");
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("display_name", out var d))
                                address = d.GetString() ?? "";
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Service] Address fail {ex.Message}");
                        }

                        if (string.IsNullOrWhiteSpace(address) || address.Length < 10)
                        {
                            // fallback MAUI
                            try
                            {
                                var place = (await Geocoding.Default.GetPlacemarksAsync(loc.Latitude, loc.Longitude))?.FirstOrDefault();
                                if (place != null)
                                    address = $"{place.Thoroughfare}, {place.Locality}, {place.AdminArea}, {place.CountryName}";
                            }
                            catch { }
                        }

                        if (string.IsNullOrWhiteSpace(address))
                            address = $"Lat {loc.Latitude}, Lon {loc.Longitude} - Lilongwe";

                        var data = new
                        {
                            MacAddress = mac,
                            CompanyCode = companyCode,
                            EmployeeCode = empCode,
                            Latitude = loc.Latitude,
                            Longitude = loc.Longitude,
                            Address = address,
                            BatteryLevel = 100,
                            DeviceOS = "Android"
                        };

                        using var client = ApiConfig.CreateClient();
                        await client.PostAsJsonAsync("api/LocationLogs", data);
                       
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Service] Fatal {ex.Message}");
                }
            }, null, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(12));

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            _timer?.Dispose();
            base.OnDestroy();
        }
    }
}
#endif
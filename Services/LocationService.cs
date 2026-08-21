using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.Maui.Devices.Sensors;

namespace FlexiPortal.Mobile.Services;

public class LocationService
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri("https://mobtrack-api.flexihrmcloud.com/api/") };
    private static Timer _autoTimer;
    private static bool _isRunning = false;
    private static bool _isSending = false;

    public async Task<string> GetOrCreateDeviceIdAsync()
    {
        var id = Preferences.Default.Get("DeviceUUID", "");
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString();
            Preferences.Default.Set("DeviceUUID", id);
        }
        return id;
    }

    public void StartAutoTracking()
    {
        if (_isRunning) return;
        StopAutoTracking();
        _isRunning = true;
#if ANDROID
        try
        {
            var context = Platform.AppContext;
            var intent = new Android.Content.Intent(context, typeof(Platforms.Android.LocationForegroundService));
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);
        }
        catch { StartTimerFallback(); }
#else
        StartTimerFallback();
#endif
    }

    void StartTimerFallback()
    {
        _autoTimer = new Timer(_ =>
        {
            if (_isSending) return;
            _isSending = true;
            Task.Run(async () =>
            {
                try
                {
                    var loc = await Geolocation.Default.GetLastKnownLocationAsync()
                            ?? await Geolocation.Default.GetLocationAsync(
                                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                    if (loc != null)
                        await SendLocationAsync(loc.Latitude, loc.Longitude, "Background-12Min");
                }
                catch { }
                finally { _isSending = false; }
            });
        }, null, TimeSpan.FromMinutes(12), TimeSpan.FromMinutes(12));
    }

    public void StopAutoTracking()
    {
#if ANDROID
        try
        {
            var context = Platform.AppContext;
            var intent = new Android.Content.Intent(context, typeof(Platforms.Android.LocationForegroundService));
            context.StopService(intent);
        }
        catch { }
#endif
        _autoTimer?.Dispose();
        _autoTimer = null;
        _isRunning = false;
        _isSending = false;
    }

    public async Task SendLocationAsync(double lat, double lon, string reason, int? numericEmpId = null)
    {
        string address = await GetAddressFromLatLong(lat, lon);
        await SendLocationAsync(lat, lon, address, reason, numericEmpId);
    }

    public async Task SendLocationAsync(double lat, double lon, string resolvedAddress, string reason, int? numericEmpId = null)
    {
        try
        {
            var mac = await GetOrCreateDeviceIdAsync();
            var companyCode = Preferences.Default.Get("CompanyCode", "").Trim().ToUpper();
            var empCode = Preferences.Default.Get("EmployeeId", "").Trim().ToUpper();
            if (string.IsNullOrEmpty(empCode))
                empCode = Preferences.Default.Get("LoggedInEmployeeId", "").Trim().ToUpper();

            if (string.IsNullOrEmpty(companyCode) || string.IsNullOrEmpty(empCode)) return;

            string finalAddress = resolvedAddress;
            if (string.IsNullOrWhiteSpace(finalAddress) || finalAddress.Length < 10 || finalAddress.Contains($"{lat}"))
            {
                finalAddress = await GetAddressFromLatLong(lat, lon);
            }

            // NEVER save lat,lon in Address column
            if (finalAddress.Contains($"{lat}") || finalAddress.Length < 10)
            {
                finalAddress = "";
            }

            if (finalAddress.Length > 450) finalAddress = finalAddress.Substring(0, 450);
            Debug.WriteLine($"[GPS] Address to save: {finalAddress}");

            var data = new
            {
                MacAddress = mac,
                CompanyCode = companyCode,
                EmployeeCode = empCode,
                Latitude = lat,
                Longitude = lon,
                Address = finalAddress,
                BatteryLevel = 100,
                DeviceOS = $"Android - {reason}",
                NumericEmpId = numericEmpId
            };
            var res = await _client.PostAsJsonAsync("LocationLogs", data);
            var body = await res.Content.ReadAsStringAsync();
            Debug.WriteLine($"[GPS] SAVED {res.StatusCode} {body}");
        }
        catch (Exception ex) { Debug.WriteLine($"[GPS] Send fail {ex}"); }
    }

    public async Task<string> GetAddressFromLatLong(double lat, double lon)
    {
        // 1. Nominatim FIRST - gives accurate 7th Cross
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "TNTIndia-Tracker/1.0");
            http.Timeout = TimeSpan.FromSeconds(8);

            var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}&zoom=18&addressdetails=1&accept-language=en";
            var jsonStr = await http.GetStringAsync(url);

            using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);

            if (doc.RootElement.TryGetProperty("address", out var a))
            {
                string G(params string[] k) { foreach (var key in k) if (a.TryGetProperty(key, out var v) && !string.IsNullOrWhiteSpace(v.GetString())) return v.GetString().Trim(); return ""; }

                string cross = G("road", "footway", "pedestrian"); // 7th Cross
                string area = G("neighbourhood", "residential", "quarter"); // Sampige Nagara
                string village = G("village", "hamlet", "locality"); // Andapura
                string town = G("town", "municipality", "suburb"); // Anekal
                string state = G("state"); // Karnataka
                string postcode = G("postcode"); // 560100

                // If road missing but display_name has Cross, extract it
                if (string.IsNullOrWhiteSpace(cross) && doc.RootElement.TryGetProperty("display_name", out var dn))
                {
                    var disp = dn.GetString() ?? "";
                    var m = System.Text.RegularExpressions.Regex.Match(disp, @"\d+(?:st|nd|rd|th)?\s+Cross", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success) cross = m.Value;
                }

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(cross)) parts.Add(cross);
                if (!string.IsNullOrWhiteSpace(area)) parts.Add(area);
                if (!string.IsNullOrWhiteSpace(village)) parts.Add(village);
                if (!string.IsNullOrWhiteSpace(town)) parts.Add(town);
                if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);
                if (!string.IsNullOrWhiteSpace(postcode)) parts.Add(postcode);
                parts.Add("India");

                var formatted = string.Join(", ", parts.Distinct().Where(s => !string.IsNullOrWhiteSpace(s)));
                if (formatted.Length > 10) return formatted.Length > 450 ? formatted.Substring(0, 450) : formatted;
            }
        }
        catch (Exception ex) { Debug.WriteLine($"[Nominatim Error]: {ex.Message}"); }

        // 2. MAUI Geocoding fallback only if Nominatim fails
        try
        {
            var places = await Geocoding.Default.GetPlacemarksAsync(lat, lon);
            var p = places?.FirstOrDefault();
            if (p != null)
            {
                var list = new List<string>();
                if (!string.IsNullOrWhiteSpace(p.Thoroughfare)) list.Add(p.Thoroughfare);
                if (!string.IsNullOrWhiteSpace(p.SubLocality)) list.Add(p.SubLocality);
                if (!string.IsNullOrWhiteSpace(p.Locality)) list.Add(p.Locality);
                if (!string.IsNullOrWhiteSpace(p.AdminArea)) list.Add(p.AdminArea);
                if (!string.IsNullOrWhiteSpace(p.CountryName)) list.Add(p.CountryName);
                var addr = string.Join(", ", list.Distinct().Where(s => !string.IsNullOrWhiteSpace(s)));
                if (addr.Length > 5) return addr;
            }
        }
        catch { }

        return "";
    }
}
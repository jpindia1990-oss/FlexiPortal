
using System.Text.Json;

namespace FlexiPortal.Mobile.Pages;

public partial class PayrollPage : ContentPage
{
    private readonly HttpClient _client;
    private List<PayItem> _allItems = new();

    public PayrollPage(HttpClient client)
    {
        InitializeComponent();
        _client = client;
        Loaded += async (s, e) => await LoadPayroll();
    }

    async Task LoadPayroll()
    {
        try
        {
            RefreshView.IsRefreshing = true;
            var token = Preferences.Default.Get("Token", "");
            var comp = Preferences.Default.Get("CompanyCode", "");
            var empId = Preferences.Default.Get("EmployeeId", "");

            if (string.IsNullOrWhiteSpace(token) || token == "mobile_token")
            {
                await DisplayAlert("Login", "Invalid token, please login again", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(comp) || string.IsNullOrWhiteSpace(empId))
            {
                await DisplayAlert("Missing", $"Company: {comp}, Emp: {empId}", "OK");
                return;
            }

            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            
            var resp = await _client.GetAsync($"api/Payslip/list?companyCode={Uri.EscapeDataString(comp)}&empId={Uri.EscapeDataString(empId)}");
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                await DisplayAlert("API Error", json, "OK");
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var rawList = JsonSerializer.Deserialize<List<RawDto>>(json, options) ?? new();

            
_allItems = rawList.Select(p => {
    var dt = ParseDate(p.E_Cal_Date);
    return new PayItem
    {
        E_Cal_DateRaw = p.E_Cal_Date,
        E_Cal_Date = dt,
        Month = dt.Month,
        Year = dt.Year, 
        MonthDisplay = dt.ToString("MMMM yyyy"),
        MonthShort = dt.ToString("MMM").ToUpper(),
        MonthYear = dt.Year * 100 + dt.Month,
        StatusText = "Paid",
        NetPay = p.NetPay,
        TotalEarnings = p.TotalEarnings ?? p.TotalEarn ?? 0,
        TotalDeductions = p.TotalDeductions ?? p.TotalDed ?? 0
    };
}).OrderByDescending(x => x.E_Cal_Date).Take(3).ToList();

            PayrollList.ItemsSource = _allItems;
            YtdEarnLabel.Text = $"₹{_allItems.Sum(x => x.TotalEarnings):N0}";
            YtdDedLabel.Text = $"₹{_allItems.Sum(x => x.TotalDeductions):N0}";
            AvgNetLabel.Text = _allItems.Any() ? $"₹{_allItems.Average(x => x.NetPay):N0}" : "₹0";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
        finally { RefreshView.IsRefreshing = false; }
    }

    DateTime ParseDate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return DateTime.Now;
        input = input.Trim();

   
        if (input.Length == 6 && int.TryParse(input, out _))
        {
            int y = int.Parse(input.Substring(0, 4));
            int m = int.Parse(input.Substring(4, 2));
            if (m >= 1 && m <= 12 && y >= 2000) return new DateTime(y, m, 1);
        }
        if (input.Length == 8 && int.TryParse(input, out _))
        {
            int y = int.Parse(input.Substring(0, 4));
            int m = int.Parse(input.Substring(4, 2));
            int d = int.Parse(input.Substring(6, 2));
            return new DateTime(y, m, d);
        }
        // 2. 05/2025
        if (input.Contains("/"))
        {
            if (DateTime.TryParseExact(input, new[] { "MM/yyyy", "M/yyyy", "dd/MM/yyyy" },
               System.Globalization.CultureInfo.InvariantCulture,
               System.Globalization.DateTimeStyles.None, out var dt2))
                return dt2;
        }
        // 3. ISO 2025-07-31T00:00:00
        if (DateTime.TryParse(input, out var dt)) return dt;

        return DateTime.Now;
    }

    async void OnRefresh(object sender, EventArgs e) => await LoadPayroll();

    async void OnPayslipTapped(object sender, TappedEventArgs e)
    {
        if ((sender as VisualElement)?.BindingContext is not PayItem item) return;

        var comp = Preferences.Default.Get("CompanyCode", "").Trim().ToUpper();
        var empId = Preferences.Default.Get("EmployeeId", "").Trim();

        // USE THIS - NOT item.MonthYear int
        string monthYearStr = item.MonthYearFormatted; // 05/2026

        try
        {
            var token = Preferences.Default.Get("Token", "");
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = $"api/Payslip/pdf?companyCode={Uri.EscapeDataString(comp)}&empId={Uri.EscapeDataString(empId)}&monthYear={Uri.EscapeDataString(monthYearStr)}";

            var resp = await _client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                await DisplayAlert($"Failed {resp.StatusCode}", err, "OK");
                return;
            }

            var pdf = await resp.Content.ReadAsByteArrayAsync();
            var path = System.IO.Path.Combine(FileSystem.CacheDirectory, $"Payslip_{empId}_{monthYearStr.Replace("/", "-")}.pdf");
            await System.IO.File.WriteAllBytesAsync(path, pdf);
            await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
        }
        catch (Exception ex) { await DisplayAlert("Failed", ex.ToString(), "OK"); }
    }
    public class RawDto
    {
        public string E_Cal_Date { get; set; } = "";
        public decimal NetPay { get; set; }
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalEarn { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal? TotalDed { get; set; }
    }

    public class PayItem
    {
        public string E_Cal_DateRaw { get; set; } = "";
        public DateTime E_Cal_Date { get; set; }
        public int MonthYear { get; set; }
        public string MonthDisplay { get; set; } = "";
        public string MonthShort { get; set; } = "";
        public string StatusText { get; set; } = "Paid";
        public decimal NetPay { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthYearFormatted => $"{Month:00}/{Year}";
    }
}
using FlexiPortal.Mobile.Helpers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlexiPortal.Mobile.Pages;

public partial class AttendanceHistoryPage : ContentPage
{
    private DateTime _current = DateTime.Now;
    private readonly HttpClient _client = ApiConfig.CreateClient();

    public AttendanceHistoryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    async void OnPrevMonth(object s, EventArgs e)
    {
        _current = _current.AddMonths(-1);
        await LoadData();
    }
    async void OnNextMonth(object s, EventArgs e)
    {
        _current = _current.AddMonths(1);
        await LoadData();
    }

    async Task LoadData()
    {
        try
        {
            IsBusy = true;
            MonthLabel.Text = _current.ToString("MMM yyyy").ToUpper();

            var empId = Preferences.Default.Get("EmployeeId", "");
            if (string.IsNullOrEmpty(empId)) empId = Preferences.Default.Get("EmployeeID", "");
            if (string.IsNullOrEmpty(empId)) empId = Preferences.Default.Get("EmpId", "");
            var company = Preferences.Default.Get("CompanyCode", "");
            if (string.IsNullOrEmpty(company)) company = Preferences.Default.Get("LoggedCompanyCode", "");
            company = company.Trim().ToUpper();

            if (string.IsNullOrEmpty(empId) || string.IsNullOrEmpty(company))
            {
                await DisplayAlert("Error", "Login again", "OK");
                return;
            }

            var url = $"api/Attendance/History/{company}/{empId}?month={_current.Month}&year={_current.Year}";
            var response = await _client.GetAsync(url);
            var raw = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("API Error", raw, "OK");
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new NullableDateTimeConverter());

            var list = JsonSerializer.Deserialize<List<HistoryDto>>(raw, options) ?? new();

            var display = list.Select(x => {
                var st = (x.Status ?? "").Trim().ToUpper();
                Color c, bg;

                if (st == "AB" || st == "A") { c = Color.FromArgb("#DC2626"); bg = Color.FromArgb("#FEE2E2"); }
                else if (st.StartsWith("WO")) { c = Color.FromArgb("#2563EB"); bg = Color.FromArgb("#DBEAFE"); }
                else if (st == "PH" || st == "HO" || st == "H") { c = Color.FromArgb("#16A34A"); bg = Color.FromArgb("#DCFCE7"); }
                else { c = Color.FromArgb("#16A34A"); bg = Color.FromArgb("#DCFCE7"); }

                // FIXED - Use .HasValue and .Value
                var inStr = x.FirstPunch.HasValue && x.In_Ti != 0 ? x.FirstPunch.Value.ToString("HH:mm") : "--";
                var outStr = x.LastPunch.HasValue && x.Out_Ti != 0 ? x.LastPunch.Value.ToString("HH:mm") : "--";

                if (st == "AB" || st == "A") { inStr = "--"; outStr = "--"; }

                return new DisplayModel
                {
                    PunchDate = x.PunchDate,
                    DayNumber = x.PunchDate.Day.ToString(),
                    DayShort = x.PunchDate.ToString("ddd").ToUpper(),
                    InTime = inStr,
                    OutTime = outStr,
                    HasOut = x.Out_Ti > 0,
                    StatusText = st,
                    StatusColor = c,
                    StatusBg = bg,
                    BgColor = st == "AB" ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#FFFFFF"),
                    TextColor = Color.FromArgb("#111827"),
                    Location = x.Location,
                    PunchCount = x.PunchCount
                };
            }).OrderByDescending(x => x.PunchDate).ToList();

            AttendanceList.ItemsSource = display;
            LateCountLabel.Text = list.Count(c => c.Late < 0).ToString();

            double totalAB = 0;
            foreach (var c in list)
            {
                var s = (c.Status ?? "").Trim().ToUpper();
                if (s == "AB") totalAB += 1;
                else if (s == "P/AB" || s.EndsWith("/AB") || s.StartsWith("AB/"))
                {
                    if (s != "AB1" && s != "AB2")
                        totalAB += 0.5;
                }
            }
            AbsentCountLabel.Text = totalAB.ToString("0.##");
            WOCountLabel.Text = list.Count(c => c.Status != null && c.Status.StartsWith("WO")).ToString();
            PHCountLabel.Text = list.Count(c => c.Status == "PH" || c.Status == "HO" || c.Status == "H").ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.ToString(), "OK");
        }
        finally { IsBusy = false; }
    }

    async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var collectionView = sender as CollectionView;
            if (e.CurrentSelection.FirstOrDefault() is not DisplayModel m) return;
            collectionView.SelectedItem = null;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Nav Error", ex.Message, "OK");
        }
    }

    private void OnLateTapped(object sender, TappedEventArgs e) => DisplayAlert("Late Policy", "As per company policy: Late > 0 mins after shift time is marked Late.", "OK");
    private void OnAbsentTapped(object sender, TappedEventArgs e) => DisplayAlert("Absent Policy", "AB / A = Absent.", "OK");
    private void OnWOTapped(object sender, TappedEventArgs e) => DisplayAlert("WO Policy", "WO and PH refer as per company policy", "OK");
    private void OnPHTapped(object sender, TappedEventArgs e) => DisplayAlert("Holiday Policy", "WO and PH refer as per company policy", "OK");

    // Converter to handle null and "" dates
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (DateTime.TryParse(s, out var dt)) return dt;
                return null;
            }
            if (reader.TokenType == JsonTokenType.Number) return null;
            try { return reader.GetDateTime(); } catch { return null; }
        }
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null) writer.WriteNullValue();
            else writer.WriteStringValue(value.Value);
        }
    }

    public class HistoryDto
    {
        public DateTime PunchDate { get; set; }
        public DateTime? LastPunch { get; set; }
        public DateTime? FirstPunch { get; set; }
        public int PunchCount { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public double In_Ti { get; set; }
        public double Out_Ti { get; set; }
        public int Late { get; set; }
    }

    public class DisplayModel
    {
        public DateTime PunchDate { get; set; }
        public string DayNumber { get; set; }
        public string DayShort { get; set; }
        public string InTime { get; set; }
        public string OutTime { get; set; }
        public bool HasOut { get; set; }
        public string StatusText { get; set; }
        public Color StatusColor { get; set; }
        public Color StatusBg { get; set; }
        public Color BgColor { get; set; }
        public Color TextColor { get; set; }
        public string Location { get; set; }
        public int PunchCount { get; set; }
    }
}
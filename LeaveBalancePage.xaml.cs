using FlexiPortal.Mobile.Services;
using Microsoft.Maui.Storage;

namespace FlexiPortal.Mobile.Pages;

public partial class LeaveBalancePage : ContentPage
{
    private readonly ApiService _apiService;
    public LeaveBalancePage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        FromDate.Date = DateTime.Today;
        ToDate.Date = DateTime.Today;
        DaysLabel.Text = "1 Day";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var types = await _apiService.GetLeaveTypesAsync();
            LeaveTypePicker.ItemsSource = types;
            LeaveTypePicker.ItemDisplayBinding = new Binding("Lv_Desc");
        }
        catch { }

        await LoadLeaveData();
    }

    private async Task LoadLeaveData()
    {
        try
        {
            string empId = Preferences.Default.Get("LoggedInEmployeeId", "");
            if (string.IsNullOrEmpty(empId)) empId = Preferences.Default.Get("EmployeeId", "");
            if (string.IsNullOrEmpty(empId)) return;

            var balances = await _apiService.GetLeaveBalanceAsync(empId);
            BalanceList.ItemsSource = balances;

            var history = await _apiService.GetLeaveHistoryAsync(empId);
            if (history == null || history.Count == 0)
            {
                NoDataLabel.IsVisible = true;
                LeaveHistoryList.ItemsSource = null;
            }
            else
            {
                NoDataLabel.IsVisible = false;
                var latest5 = history
                   .OrderByDescending(h => h.Ent_date)
                   .Take(5)
                   .Select(h => new LeaveHistoryModel
                   {
                       TranId = h.Tran_Id, // <-- IMPORTANT FOR DELETE
                       LeaveType = h.Lv_type,
                       Dates = $"{h.Fr_date:dd MMM} - {h.To_date:dd MMM} ({h.Lv_days} days)",
                       Reason = h.Lv_reason,
                       Status = h.Lv_Stat,
                       StatusColor = h.Lv_Stat == "Approved" ? Colors.Green : h.Lv_Stat == "Rejected" ? Colors.Red : Colors.Orange
                   }).ToList();

                LeaveHistoryList.ItemsSource = latest5;
            }
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        int tranId = Convert.ToInt32(btn.CommandParameter);
        if (!await DisplayAlert("Delete", $"Delete {tranId}?", "Yes", "No")) return;

        var resp = await _apiService.DeleteLeaveAsyncWithMessage(tranId);
        if (resp.success)
        {
            await DisplayAlert("Deleted", "Deleted", "OK");
            await LoadLeaveData();
        }
        else
        {
            await DisplayAlert("Error", resp.message, "OK");
        }
    }
    private async void OnDateChanged(object sender, DateChangedEventArgs e)
    {
        if (HalfDayCheck.IsChecked)
        {
            ToDate.Date = FromDate.Date;
            LeaveDaysEntry.Text = "0.5";
            DaysLabel.Text = "0.5 Day";
            return;
        }
        try
        {
            string empId = Preferences.Default.Get("LoggedInEmployeeId", "");
            if (string.IsNullOrEmpty(empId)) empId = Preferences.Default.Get("EmployeeId", "");
            double days = await _apiService.GetWorkingDaysAsync(empId, FromDate.Date, ToDate.Date);
            LeaveDaysEntry.Text = days.ToString();
            DaysLabel.Text = $"{days} Day{(days > 1 ? "s" : "")}";
        }
        catch
        {
            var days = (ToDate.Date - FromDate.Date).Days + 1;
            if (days < 1) days = 1;
            LeaveDaysEntry.Text = days.ToString();
            DaysLabel.Text = $"{days} Day{(days > 1 ? "s" : "")}";
        }
    }

    private void OnHalfDayChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            ToDate.Date = FromDate.Date;
            ToDate.IsEnabled = false;
            LeaveDaysEntry.Text = "0.5";
            DaysLabel.Text = "0.5 Day";
        }
        else
        {
            ToDate.IsEnabled = true;
            OnDateChanged(null, null);
        }
    }

    private async void OnSubmitLeaveClicked(object sender, EventArgs e)
    {
        if (LeaveTypePicker.SelectedIndex < 0 || LeaveTypePicker.SelectedItem == null)
        {
            await DisplayAlert("Validation", "From Date, To Date & Leave Type is Mandatory", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(ReasonEditor.Text))
        {
            await DisplayAlert("Validation", "Reason is required", "OK");
            return;
        }

        var btn = (Button)sender;
        btn.IsEnabled = false;
        btn.Text = "Updating...";

        try
        {
            string empId = Preferences.Default.Get("LoggedInEmployeeId", "");
            if (string.IsNullOrEmpty(empId)) empId = Preferences.Default.Get("EmployeeId", "");

            string fr = FromDate.Date.ToString("dd/MM/yyyy");
            string to = ToDate.Date.ToString("dd/MM/yyyy");
            var selectedType = LeaveTypePicker.SelectedItem as ApiService.LeaveTypeDto;
            string lvType = selectedType.Lv_Desc;
            double lvDays = double.Parse(LeaveDaysEntry.Text);

            string tran_id1 = "";
            if (HalfDayCheck.IsChecked)
            {
                tran_id1 = $" having sum(Lv_days) >= 1 or max(Lv_type) = '{lvType}'";
            }

            string Tran_Id = "New";

            var success = await _apiService.SubmitLeaveEntryAsync(empId, fr, to, lvType, lvDays, Tran_Id, tran_id1, ReasonEditor.Text);

            await DisplayAlert("Success", $"Leave request for {lvDays} day(s) submitted!", "OK");
            ReasonEditor.Text = string.Empty;
            HalfDayCheck.IsChecked = false;
            await LoadLeaveData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            btn.IsEnabled = true;
            btn.Text = "Update";
        }
    }

  
    private void OnCancelClicked(object sender, EventArgs e)
    {
        ReasonEditor.Text = string.Empty;
        LeaveTypePicker.SelectedIndex = -1;
        HalfDayCheck.IsChecked = false;
        FromDate.Date = DateTime.Today;
        ToDate.Date = DateTime.Today;
        LeaveDaysEntry.Text = "1";
        DaysLabel.Text = "1 Day";
        ToDate.IsEnabled = true;
    }

    public class LeaveHistoryModel
    {
        public int TranId { get; set; }
        public string LeaveType { get; set; }
        public string Dates { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public Color StatusColor { get; set; }
        public bool IsPending => Status == "Pending" || Status == "P";
    }
}
using FlexiPortal.Mobile.Services;

namespace FlexiPortal.Mobile.Pages;

public partial class LeaveApprovalPage : ContentPage
{
    private readonly ApiService _apiService;
    public LeaveApprovalPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPending();
    }

    private string GetMgrId()
    {
        var id = Preferences.Default.Get("LoggedInEmployeeId", "");
        if (string.IsNullOrEmpty(id)) id = Preferences.Default.Get("EmployeeId", "");
        return id;
    }

    private async Task LoadPending()
    {
        try
        {
            Loading.IsVisible = true;
            var pending = await _apiService.GetPendingLeavesAsync(GetMgrId());
            ApprovalList.ItemsSource = pending;
            NoDataLabel.IsVisible = pending == null || pending.Count == 0;
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        finally { Loading.IsVisible = false; }
    }

    private async Task LoadHistory()
    {
        try
        {
            Loading.IsVisible = true;
            var all = await _apiService.GetAllLeavesAsync(GetMgrId());
            HistoryList.ItemsSource = all;
            NoDataLabel.IsVisible = all == null || all.Count == 0;
        }
        catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        finally { Loading.IsVisible = false; }
    }

    void OnPendingTab(object sender, EventArgs e)
    {
        ApprovalList.IsVisible = true;
        HistoryList.IsVisible = false;
        BtnPending.BackgroundColor = Color.FromArgb("#0A84FF");
        BtnPending.TextColor = Colors.White;
        BtnHistory.BackgroundColor = Colors.White;
        BtnHistory.TextColor = Colors.Black;
        _ = LoadPending();
    }

    void OnHistoryTab(object sender, EventArgs e)
    {
        ApprovalList.IsVisible = false;
        HistoryList.IsVisible = true;
        BtnHistory.BackgroundColor = Color.FromArgb("#0A84FF");
        BtnHistory.TextColor = Colors.White;
        BtnPending.BackgroundColor = Colors.White;
        BtnPending.TextColor = Colors.Black;
        _ = LoadHistory();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        if (ApprovalList.IsVisible) await LoadPending();
        else await LoadHistory();
    }

    private async void OnApproveClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        int tranId = Convert.ToInt32(btn.CommandParameter);
        if (!await DisplayAlert("Approve", $"Approve {tranId}?", "Yes", "No")) return;
        bool ok = await _apiService.ApproveLeaveAsync(tranId, GetMgrId(), "Approved");
        if (ok) await LoadPending();
    }

    private async void OnRejectClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        int tranId = Convert.ToInt32(btn.CommandParameter);
        if (await DisplayPromptAsync("Reject", "Reason?") == null) return;
        bool ok = await _apiService.ApproveLeaveAsync(tranId, GetMgrId(), "Rejected");
        if (ok) await LoadPending();
    }
}
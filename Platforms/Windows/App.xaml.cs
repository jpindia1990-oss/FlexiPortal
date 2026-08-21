using FlexiPortal.Mobile;

namespace FlexiPortal.Mobile.WinUI;

public partial class App : MauiWinUIApplication
{
    public App() => InitializeComponent();
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
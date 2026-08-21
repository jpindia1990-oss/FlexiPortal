
using FlexiPortal.Mobile;
using FlexiPortal.Mobile.Pages;
using FlexiPortal.Mobile.Services;
using Microsoft.Extensions.Logging;
using FlexiPortal.Mobile.Helpers;
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
		   .UseMauiApp<App>()
		   .ConfigureFonts(fonts =>
		   {
			   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			   fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
		   });

		builder.Services.AddTransient<AuthHeaderHandler>();

        // FIXED: Use HttpClientFactory - this avoids "inner handler has been assigned"
        builder.Services.AddHttpClient("Api", client =>
        {
            client.BaseAddress = new Uri(ApiConfig.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<AuthHeaderHandler>();

        builder.Services.AddSingleton<HttpClient>(sp =>
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://mobtrack-api.flexihrmcloud.com/")
            };
        });

        builder.Services.AddSingleton<ApiService>();
		builder.Services.AddSingleton<LocationService>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<AppShell>();
		builder.Services.AddTransient<PayrollPage>();
     

#if DEBUG
        builder.Logging.AddDebug();
#endif
		return builder.Build();
	}
}
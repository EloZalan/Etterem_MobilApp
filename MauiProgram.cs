using CommunityToolkit.Maui;
using MauiApp1;
using Microsoft.Extensions.Logging;
using WaiterApp.Services;
using WaiterApp.Services.Payments;
using WaiterApp.ViewModels;
using WaiterApp.Views;

namespace WaiterApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        // MauiProgram.cs – platform-specifikus NFC regisztráció
#if ANDROID
        builder.Services.AddSingleton<INfcPaymentService, WaiterApp.Platforms.Android.Services.AndroidNfcPaymentService>();
#else
        builder.Services.AddSingleton<INfcPaymentService, DefaultNfcPaymentService>();
#endif

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<TablesPage>();
        builder.Services.AddTransient<TableDetailsPage>();
        builder.Services.AddTransient<PayPage>();

        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddSingleton<TablesViewModel>();
        builder.Services.AddTransient<TableDetailsViewModel>();
        builder.Services.AddTransient<PayPageViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

using CommunityToolkit.Maui;
using MauiApp1;
using Microsoft.Extensions.Logging;
using WaiterApp.Services;
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

using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using WaiterApp.Services;
namespace WaiterApp;

public partial class App : Application
{
    private bool _isLoggingOut;
    public App(AppShell shell)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;

        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        MainPage = shell;
    }

    private async void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        await Task.Run(async () => await Task.Delay(500));
        if (e.NetworkAccess != NetworkAccess.Internet)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Hiba!", "Nem elerheto az internet a telefonjan", "Ok");
            });
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);

        window.Deactivated += OnWindowDeactivated;
        window.Stopped += OnWindowStopped;

        return window;
    }

    private async void OnWindowDeactivated(object? sender, EventArgs e)
    {
        await LogoutIfAppLostFocusAsync();
    }

    private async void OnWindowStopped(object? sender, EventArgs e)
    {
        await LogoutIfAppLostFocusAsync();
    }

    private async Task LogoutIfAppLostFocusAsync()
    {
        if (_isLoggingOut)
            return;

        var authService = Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthService>();

        if (authService == null || string.IsNullOrWhiteSpace(authService.Token))
            return;

        _isLoggingOut = true;

        try
        {
            await authService.LogoutAsync();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("//login");
            });
        }
        finally
        {
            _isLoggingOut = false;
        }
    }
}
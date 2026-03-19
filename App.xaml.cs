using Microsoft.Maui.Controls;

namespace WaiterApp;

public partial class App : Application
{
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
}
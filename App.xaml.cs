using Microsoft.Maui.Controls;

namespace WaiterApp;

public partial class App : Application
{
    public App(AppShell shell)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;

        MainPage = shell;
    }
}
using WaiterApp.Views;

namespace WaiterApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(TableDetailsPage), typeof(TableDetailsPage));
        Routing.RegisterRoute(nameof(PayPage), typeof(PayPage));
    }
}

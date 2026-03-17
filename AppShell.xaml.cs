using WaiterApp.Views;

namespace WaiterApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(TableDetailsPage), typeof(TableDetailsPage));
    }
}

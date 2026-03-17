using WaiterApp.ViewModels;

namespace WaiterApp.Views;

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;

    public TablesPage(TablesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}

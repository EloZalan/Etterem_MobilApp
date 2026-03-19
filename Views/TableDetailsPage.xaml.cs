using WaiterApp.Models;
using WaiterApp.ViewModels;

namespace WaiterApp.Views;

public partial class TableDetailsPage : ContentPage, IQueryAttributable
{
    private readonly TableDetailsViewModel _viewModel;

    public TableDetailsPage(TableDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("SelectedTable", out var value) && value is RestaurantTable table)
        {
            _viewModel.SelectedTable = table;
        }

        if (query.TryGetValue("AutoOpenOrder", out var autoOpen) && autoOpen is bool shouldAutoOpen)
        {
            _viewModel.AutoOpenOrder = shouldAutoOpen;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void PayByCash_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PayPage));
    }

    private void PayByCard_Clicked(object sender, EventArgs e)
    {

    }
}
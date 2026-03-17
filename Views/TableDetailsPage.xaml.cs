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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}

using WaiterApp.Models;
using WaiterApp.ViewModels;

namespace WaiterApp.Views;

public partial class PayPage : ContentPage, IQueryAttributable
{
    private readonly PayPageViewModel _viewModel;

    public PayPage(PayPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("CurrentOrder", out var currentOrder) && currentOrder is Order order)
        {
            _viewModel.CurrentOrder = order;
        }
    }
}

//using Android.Webkit;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WaiterApp.Models;
using WaiterApp.Services;

namespace WaiterApp.ViewModels;

[QueryProperty(nameof(SelectedTable), nameof(SelectedTable))]
public class TableDetailsViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private RestaurantTable? _selectedTable;
    private Order? _currentOrder;
    private MenuCategory? _selectedCategory;
    private WaiterMenuItems? _selectedMenuItem;
    private int _quantity = 1;
    private string _statusMessage = "Open an order for this table or continue adding items.";

    public TableDetailsViewModel(IApiService apiService)
    {
        _apiService = apiService;
        Categories = new ObservableCollection<MenuCategory>();
        MenuItems = new ObservableCollection<WaiterMenuItems>();

        LoadCommand = new Command(async () => await LoadAsync());
        OpenOrderCommand = new Command(async () => await OpenOrderAsync());
        AddItemCommand = new Command(async () => await AddItemAsync());
        MarkReadyCommand = new Command(async () => await MarkReadyAsync());
        PayCashCommand = new Command(async () => await PayAsync("cash"));
        PayCardCommand = new Command(async () => await PayAsync("card"));
        IncreaseQuantityCommand = new Command(() => Quantity++);
        DecreaseQuantityCommand = new Command(() => { if (Quantity > 1) Quantity--; });
    }

    public RestaurantTable? SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                Title = value is null ? "Table" : $"Table {value.Id}";
            }
        }
    }

    public Order? CurrentOrder
    {
        get => _currentOrder;
        set => SetProperty(ref _currentOrder, value);
    }

    public ObservableCollection<MenuCategory> Categories { get; }
    public ObservableCollection<WaiterMenuItems> MenuItems { get; }

    public MenuCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                FilterMenuItems();
        }
    }

    public WaiterMenuItems? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => SetProperty(ref _selectedMenuItem, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool HasOrder => CurrentOrder is not null;
    public string CurrentOrderLabel => CurrentOrder is null
        ? "No open order"
        : $"Order #{CurrentOrder.Id} • {CurrentOrder.Status} • {CurrentOrder.TotalPriceLabel}";

    public ICommand LoadCommand { get; }
    public ICommand OpenOrderCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand MarkReadyCommand { get; }
    public ICommand PayCashCommand { get; }
    public ICommand PayCardCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Loading menu...";

        try
        {
            Categories.Clear();
            MenuItems.Clear();

            var categories = await _apiService.GetMenuCategoriesAsync();
            foreach (var category in categories.OrderBy(c => c.Name))
                Categories.Add(category);

            _allMenuItems = await _apiService.GetMenuItemsAsync();
            SelectedCategory = Categories.FirstOrDefault();

            StatusMessage = "The backend swagger does not provide an endpoint to fetch existing order details. This screen can open a new order and add items, but reloading an already existing order is not possible without a new GET endpoint.";
            RefreshComputedProperties();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private List<WaiterMenuItems> _allMenuItems = new();

    private void FilterMenuItems()
    {
        MenuItems.Clear();
        var items = _allMenuItems
            .Where(i => SelectedCategory is null || i.CategoryId == SelectedCategory.Id)
            .OrderBy(i => i.Name);

        foreach (var item in items)
            MenuItems.Add(item);

        SelectedMenuItem = MenuItems.FirstOrDefault();
    }

    private async Task OpenOrderAsync()
    {
        if (SelectedTable is null)
            return;

        try
        {
            CurrentOrder = await _apiService.OpenOrderForTableAsync(SelectedTable.Id);
            StatusMessage = "Order opened successfully.";
            RefreshComputedProperties();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task AddItemAsync()
    {
        if (CurrentOrder is null)
        {
            StatusMessage = "Open an order first.";
            return;
        }

        if (SelectedMenuItem is null)
        {
            StatusMessage = "Select a menu item.";
            return;
        }

        try
        {
            await _apiService.AddOrderItemAsync(CurrentOrder.Id, new AddOrderItemRequest
            {
                MenuItemId = SelectedMenuItem.Id,
                Quantity = Quantity
            });

            CurrentOrder.TotalPrice += SelectedMenuItem.Price * Quantity;
            StatusMessage = $"Added {Quantity} x {SelectedMenuItem.Name}.";
            RefreshComputedProperties();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task MarkReadyAsync()
    {
        if (CurrentOrder is null)
        {
            StatusMessage = "No order is open.";
            return;
        }

        try
        {
            await _apiService.SimulateReadyAsync(CurrentOrder.Id);
            CurrentOrder.Status = "ready_to_pay";
            StatusMessage = "Order marked as ready to pay.";
            RefreshComputedProperties();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task PayAsync(string method)
    {
        if (CurrentOrder is null)
        {
            StatusMessage = "No order is open.";
            return;
        }

        try
        {
            var payment = await _apiService.PayOrderAsync(CurrentOrder.Id, new PayOrderRequest
            {
                PaymentMethod = method
            });

            CurrentOrder.Status = payment.OrderStatus;
            StatusMessage = $"Payment successful with {method}.";
            RefreshComputedProperties();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(HasOrder));
        OnPropertyChanged(nameof(CurrentOrderLabel));
    }
}

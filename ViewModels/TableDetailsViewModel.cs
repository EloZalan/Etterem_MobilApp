using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using System.Windows.Input;
using WaiterApp.Models;
using WaiterApp.Services;
using WaiterApp.Views;

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
        OrderItems = new ObservableCollection<TableOrderItem>();

        LoadCommand = new Command(async () => await LoadAsync());
        OpenOrderCommand = new Command(async () => await OpenOrderAsync());
        AddItemCommand = new Command(async () => await AddItemAsync());
        DeleteOrderItemCommand = new Command<TableOrderItem>(async item => await DeleteOrderItemAsync(item));
        MarkReadyCommand = new Command(async () => await MarkReadyAsync());
        GoToPayCommand = new Command<string>(async method => await GoToPay(method ?? string.Empty));
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
        set
        {
            if (SetProperty(ref _currentOrder, value))
                RefreshComputedProperties();
        }
    }

    public ObservableCollection<MenuCategory> Categories { get; }
    public ObservableCollection<WaiterMenuItems> MenuItems { get; }
    public ObservableCollection<TableOrderItem> OrderItems { get; }

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
        set
        {
            if (value < 1)
                return;

            SetProperty(ref _quantity, value);
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool HasOrder => CurrentOrder is not null;
    public bool ShowOpenOrderButton => CurrentOrder is null;
    public bool HasOrderItems => OrderItems.Count > 0;
    public string CurrentOrderLabel => CurrentOrder is null
        ? "Nincs megnyitva az asztal"
        : $"Rendelés #{CurrentOrder.Id} • {CurrentOrder.Status} • {CurrentOrder.TotalPriceLabel}";

    public ICommand LoadCommand { get; }
    public ICommand OpenOrderCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand DeleteOrderItemCommand { get; }
    public ICommand MarkReadyCommand { get; }
    public ICommand IncreaseQuantityCommand { get; }
    public ICommand DecreaseQuantityCommand { get; }
    public ICommand GoToPayCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Loading menu...";

        try
        {
            await LoadStaticDataAsync();
            await RefreshOrderSnapshotAsync();
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

    public async Task RefreshOrderSnapshotAsync()
    {
        try
        {
            if (SelectedTable is not null)
            {
                var existingOrder = await _apiService.GetCurrentOrderForTableAsync(SelectedTable.Id);

                if (existingOrder?.HasOpenOrder == true)
                {
                    if (CurrentOrder is null || CurrentOrder.Id != existingOrder.OrderId)
                    {
                        CurrentOrder = new Order
                        {
                            Id = existingOrder.OrderId!.Value,
                            TableId = existingOrder.TableId ?? SelectedTable.Id,
                            ReservationId = existingOrder.ReservationId ?? 0,
                            TotalPrice = existingOrder.TotalPrice,
                            Status = existingOrder.Status ?? "in_progress"
                        };
                    }

                    await RefreshOrderDetailsAsync();
                    RefreshComputedProperties();
                    return;
                }
            }

            CurrentOrder = null;
            ClearOrderItems();
            RefreshComputedProperties();

            if (AutoOpenOrder && SelectedTable is not null)
            {
                AutoOpenOrder = false;
                await OpenOrderAsync();
            }
            else
            {
                StatusMessage = "Open an order for this table or continue adding items.";
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = ex.Message;
            });
        }
    }

    private async Task LoadStaticDataAsync()
    {
        if (Categories.Count > 0 && _allMenuItems.Count > 0)
            return;

        var categories = await _apiService.GetMenuCategoriesAsync();
        var menuItems = await _apiService.GetMenuItemsAsync();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Categories.Clear();
            foreach (var category in categories.OrderBy(c => c.Name))
                Categories.Add(category);

            _allMenuItems = menuItems;

            if (SelectedCategory is null || Categories.All(c => c.Id != SelectedCategory.Id))
                SelectedCategory = Categories.FirstOrDefault();
            else
                FilterMenuItems();
        });
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

    public async Task OpenOrderAsync()
    {
        if (SelectedTable is null)
            return;

        try
        {
            CurrentOrder = await _apiService.OpenOrderForTableAsync(SelectedTable.Id);
            await RefreshOrderDetailsAsync();
            StatusMessage = $"Order opened for table {SelectedTable.Id}.";
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

            await RefreshOrderDetailsAsync();
            StatusMessage = $"Added {Quantity} x {SelectedMenuItem.Name}.";
            RefreshComputedProperties();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.ToString(), "OK");
        }
    }

    private async Task DeleteOrderItemAsync(TableOrderItem? item)
    {
        if (CurrentOrder is null)
        {
            StatusMessage = "No order is open.";
            return;
        }

        if (item is null)
            return;

        var confirm = await Shell.Current.DisplayAlert(
            "Tétel törlés",
            $"Törli a(z) {item.Name} a rendelésből?",
            "Igen",
            "Nem");

        if (!confirm)
            return;

        try
        {
            await _apiService.DeleteOrderItemAsync(CurrentOrder.Id, item.Id);
            await RefreshOrderDetailsAsync();
            StatusMessage = $"{item.Name} deleted from the order.";
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
            StatusMessage = "Nincs nyitott rendelés.";
            return;
        }

        try
        {
            await _apiService.SimulateReadyAsync(CurrentOrder.Id);
            CurrentOrder.Status = "Fizetésre kész";
            await RefreshOrderDetailsAsync();
            StatusMessage = "Késznek jelentve.";
            RefreshComputedProperties();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private async Task GoToPay(string method)
    {
        if (CurrentOrder is null)
        {
            StatusMessage = "Nyiss meg egy rendelést ehhez";
            return;
        }

        await Shell.Current.GoToAsync(nameof(PayPage), new Dictionary<string, object>
        {
            ["CurrentOrder"] = CurrentOrder,
            ["PaymentMethod"] = method
        });
    }
   


    private async Task RefreshOrderDetailsAsync()
    {
        if (SelectedTable is null)
        {
            ClearOrderItems();
            return;
        }

        var details = await _apiService.GetCurrentOrderForTableAsync(SelectedTable.Id);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            TableOrderDetails = details;

            OrderItems.Clear();
            foreach (var item in details?.Items ?? Enumerable.Empty<TableOrderItem>())
                OrderItems.Add(item);

            if (details?.OrderId is not null && CurrentOrder is not null)
            {
                CurrentOrder.TotalPrice = details.TotalPrice;
                CurrentOrder.Status = details.Status ?? CurrentOrder.Status;
            }

            OnPropertyChanged(nameof(HasOrderItems));
            OnPropertyChanged(nameof(CurrentOrderLabel));
        });
    }

    private void ClearOrderItems()
    {
        TableOrderDetails = null;
        OrderItems.Clear();
        OnPropertyChanged(nameof(HasOrderItems));
    }

    private bool _autoOpenOrder;
    public bool AutoOpenOrder
    {
        get => _autoOpenOrder;
        set => SetProperty(ref _autoOpenOrder, value);
    }

    private TableOrderDetails? _tableOrderDetails;
    public TableOrderDetails? TableOrderDetails
    {
        get => _tableOrderDetails;
        set => SetProperty(ref _tableOrderDetails, value);
    }

    private void RefreshComputedProperties()
    {
        OnPropertyChanged(nameof(HasOrder));
        OnPropertyChanged(nameof(ShowOpenOrderButton));
        OnPropertyChanged(nameof(CurrentOrderLabel));
        OnPropertyChanged(nameof(HasOrderItems));
    }
}

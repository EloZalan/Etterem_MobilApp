using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using WaiterApp.Models;
using WaiterApp.Services;
using WaiterApp.Views;

namespace WaiterApp.ViewModels;

public class TablesViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;
    private string _statusMessage = string.Empty;

    public TablesViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        Title = "My Tables";
        Tables = new ObservableCollection<RestaurantTable>();
        LoadCommand = new Command(async () => await LoadAsync());
        LogoutCommand = new Command(async () => await LogoutAsync());
        DropShiftCommand = new Command(async () => await DropShiftAsync());
        TakeShiftCommand = new Command(async () => await TakeShiftAsync());
        OpenTableCommand = new Command<RestaurantTable>(async table => await OpenTableAsync(table));
    }

    public ObservableCollection<RestaurantTable> Tables { get; }
    public ObservableCollection<RestaurantTable> BusyTables { get; } = new();
    public ObservableCollection<RestaurantTable> AvailableTables { get; } = new();

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string WaiterName => _authService.CurrentUser?.Name ?? "Waiter";

    public ICommand LoadCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand DropShiftCommand { get; }
    public ICommand TakeShiftCommand { get; }
    public ICommand OpenTableCommand { get; }

    public class ApiError
    {
        public string Message { get; set; } = string.Empty;
    }
    public async Task LoadAsync()
    {


        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var allTables = await _apiService.GetTablesAsync();

            var currentWaiterId = _authService.CurrentUser?.Id;
            var visibleTables = allTables
                .Where(t => t.WaiterId is null || t.WaiterId == currentWaiterId)
                .ToList();

            SplitTables(visibleTables);

            if (Tables.Count == 0)
                StatusMessage = "No tables assigned to this waiter.";
        }
        catch (Exception ex)
        {
            StatusMessage = Encoding.UTF8.GetString(Encoding.Default.GetBytes(ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenTableAsync(RestaurantTable? table)
    {
        if (table is null)
            return;

        await Shell.Current.GoToAsync(nameof(TableDetailsPage), new Dictionary<string, object>
        {
            ["SelectedTable"] = table
        });
    }

    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
        IsOnShift = false;
    }

    private async Task DropShiftAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await _apiService.DropShiftAsync();
            IsOnShift = false;

            StatusMessage = "Shift dropped successfully.";
            await LoadAsync();
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

    private async Task TakeShiftAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await _apiService.TakeShiftAsync();
            IsOnShift = true;

            StatusMessage = "Shift taken successfully.";
            await LoadAsync();
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

    public async Task LoadTablesOnlyAsync()
    {
        Tables.Clear();
        var allTables = await _apiService.GetTablesAsync();

        var currentWaiterId = _authService.CurrentUser?.Id;
        var visibleTables = allTables
            .Where(t => t.WaiterId is null || t.WaiterId == currentWaiterId)
            .OrderBy(t => t.Id)
            .ToList();

        foreach (var table in visibleTables)
            Tables.Add(table);
    }

    public async Task CreateWalkInReservationAndOpenOrderAsync(int guestCount)
    {
        if (IsBusy)
            return;

        if (!IsOnShift)
        {
            StatusMessage = "You must take shift before creating a walk-in reservation.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var reservation = await _apiService.CreateWalkInReservationAsync(guestCount);

            await LoadTablesOnlyAsync();

            StatusMessage = $"Reservation created successfully. Table {reservation.TableId} is reserved for {guestCount} guest(s).";

            var table = Tables.FirstOrDefault(t => t.Id == reservation.TableId);
            if (table is not null)
            {
                await Shell.Current.GoToAsync(nameof(TableDetailsPage), new Dictionary<string, object>
                {
                    ["SelectedTable"] = table,
                    ["AutoOpenOrder"] = true
                });
            }
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

    private void SplitTables(IEnumerable<RestaurantTable> tables)
    {
        BusyTables.Clear();
        AvailableTables.Clear();

        foreach (var table in tables.OrderBy(t => t.Id))
        {
            var status = table.Status?.Trim().ToLowerInvariant();

            if (status == "reserved" || status == "occupied")
            {
                BusyTables.Add(table);
            }
            else
            {
                AvailableTables.Add(table);
            }
        }
    }

    private bool _isOnShift = false;

    public bool IsOnShift
    {
        get => _isOnShift;
        set => SetProperty(ref _isOnShift, value);
    }
}

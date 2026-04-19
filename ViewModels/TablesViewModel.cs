using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Maui.ApplicationModel;
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
        Title = "Asztalaim";
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

    
    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            await RefreshTablesAsync(updateBusyState: false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshTablesAsync(bool updateBusyState = false)
    {
        if (updateBusyState && IsBusy)
            return;

        if (updateBusyState)
            IsBusy = true;

        try
        {
            var allTables = await _apiService.GetTablesAsync();

            var currentWaiterId = _authService.CurrentUser?.Id;
            var visibleTables = allTables
                .Where(t => t.WaiterId is null || t.WaiterId == currentWaiterId)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SplitTables(visibleTables);

                if (BusyTables.Count == 0 && AvailableTables.Count == 0)
                    StatusMessage = "No tables assigned to this waiter.";
                else if (StatusMessage == "No tables assigned to this waiter.")
                    StatusMessage = string.Empty;
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = Encoding.UTF8.GetString(Encoding.Default.GetBytes(ex.Message));
            });
        }
        finally
        {
            if (updateBusyState)
                IsBusy = false;
        }
    }

    private async Task OpenTableAsync(RestaurantTable? table)
    {
        if (table is null)
            return;

        var reservationStart = GetReservationStartInLocalTime(table);
        if (reservationStart.HasValue && reservationStart.Value > DateTime.Now)
        {
            var reservationTimeText = (reservationStart.Value).AddHours(2).ToString("HH:mm");
            await Shell.Current.DisplayAlert(
                "A foglalás nem aktív",
                $"A foglalás {reservationTimeText} órára van idõzitve. Még nem tudod megnyitni az asztalt.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(TableDetailsPage), new Dictionary<string, object>
        {
            ["SelectedTable"] = table
        });
    }

    private static DateTime? GetReservationStartInLocalTime(RestaurantTable table)
    {
        var start = table.Reservation?.StartTime;
        if (!start.HasValue)
            return null;

        return start.Value.Kind switch
        {
            DateTimeKind.Utc => start.Value.ToLocalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(start.Value, DateTimeKind.Local),
            _ => start.Value
        };
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
            await LoadAsync();
            StatusMessage = "Shift dropped successfully.";
            IsOnShift = false;

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
            await LoadAsync();
            await Shell.Current.DisplayAlert("", "Sikeresen munkába állt.", "OK");
            StatusMessage = "Sikeres";
            IsOnShift = true;

        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Hiba", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
        await LoadAsync();
    }

    public async Task LoadTablesOnlyAsync()
    {
        var allTables = await _apiService.GetTablesAsync();

        var currentWaiterId = _authService.CurrentUser?.Id;
        var visibleTables = allTables
            .Where(t => t.WaiterId is null || t.WaiterId == currentWaiterId)
            .OrderBy(t => t.Id)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Tables.Clear();
            foreach (var table in visibleTables)
                Tables.Add(table);
        });
    }

    public async Task CreateWalkInReservationAndOpenOrderAsync(int guestCount)
    {
        if (IsBusy)
            return;

        if (!IsOnShift)
        {
            StatusMessage = "Elösszõr vedd fel a mûszakot";
            return;
        }

        var hasEnoughCapacity = AvailableTables.Any(t => t.Capacity >= guestCount);
        if (!hasEnoughCapacity)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Foglalás error",
                    $"Nem lehet {guestCount} fõre foglalni, mert nincs ennyi férõhelyes szabad asztal.",
                    "OK");
            });

            StatusMessage = $"Nincs asztal {guestCount} fõre.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var reservation = await _apiService.CreateWalkInReservationAsync(guestCount);

            await LoadTablesOnlyAsync();

            StatusMessage = $"Sikeres foglalás. Asztal {reservation.TableId} készen áll {guestCount} fõre.";

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

        await LoadAsync();
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

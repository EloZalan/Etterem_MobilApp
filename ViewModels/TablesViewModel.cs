using System.Collections.ObjectModel;
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
        OpenTableCommand = new Command<RestaurantTable>(async table => await OpenTableAsync(table));
    }

    public ObservableCollection<RestaurantTable> Tables { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string WaiterName => _authService.CurrentUser?.Name ?? "Waiter";

    public ICommand LoadCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand OpenTableCommand { get; }

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            Tables.Clear();
            var allTables = await _apiService.GetTablesAsync();

            // Swagger does not define a waiter-table ownership field or endpoint.
            // If backend later returns waiterId, this filter will start working automatically.
            var currentWaiterId = _authService.CurrentUser?.Id;
            var visibleTables = allTables
                .Where(t => t.WaiterId is null || t.WaiterId == currentWaiterId)
                .OrderBy(t => t.Id)
                .ToList();

            foreach (var table in visibleTables)
                Tables.Add(table);

            if (Tables.Count == 0)
                StatusMessage = "No tables assigned to this waiter.";
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
    }
}

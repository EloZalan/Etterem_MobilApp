using WaiterApp.ViewModels;

namespace WaiterApp.Views;

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;
    private CancellationTokenSource? _refreshCancellationTokenSource;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

    public TablesPage(TablesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
        StartAutoRefresh();
    }

    protected override void OnDisappearing()
    {
        StopAutoRefresh();
        base.OnDisappearing();
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        _refreshCancellationTokenSource = new CancellationTokenSource();
        _ = RunAutoRefreshAsync(_refreshCancellationTokenSource.Token);
    }

    private void StopAutoRefresh()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource?.Dispose();
        _refreshCancellationTokenSource = null;
    }

    private async Task RunAutoRefreshAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(RefreshInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await _viewModel.RefreshTablesAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void OnWalkInReservationClicked(object sender, EventArgs e)
    {
        var input = await DisplayPromptAsync(
            "Pincér foglalás",
            "Személyek száma:",
            accept: "OK",
            cancel: "Mégse",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(input))
            return;

        if (!int.TryParse(input, out var guestCount) || guestCount < 1)
        {
            await DisplayAlert("Hiba", "A megadható személyek száma minimum 1", "OK");
            return;
        }

        await _viewModel.CreateWalkInReservationAndOpenOrderAsync(guestCount);
    }
}
using WaiterApp.Views;
using System.Windows.Input;
using WaiterApp.Models;
using WaiterApp.ViewModels;

namespace WaiterApp.Views;

public partial class TableDetailsPage : ContentPage, IQueryAttributable
{
    private readonly TableDetailsViewModel _viewModel;
    private CancellationTokenSource? _refreshCancellationTokenSource;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);

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
                await _viewModel.RefreshOrderSnapshotAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
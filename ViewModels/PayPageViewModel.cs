using System.Windows.Input;
using WaiterApp.Models;
using WaiterApp.Services;

namespace WaiterApp.ViewModels;

public class PayPageViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private Order? _currentOrder;
    private string _statusMessage = "Choose a payment method.";

    public PayPageViewModel(IApiService apiService)
    {
        _apiService = apiService;
        PayCashCommand = new Command(async () => await PayAsync("cash"), () => !IsBusy);
        PayCardCommand = new Command(async () => await PayAsync("card"), () => !IsBusy);
    }

    public Order? CurrentOrder
    {
        get => _currentOrder;
        set
        {
            if (SetProperty(ref _currentOrder, value))
            {
                StatusMessage = value is null
                    ? "No order selected."
                    : $"Order #{value.Id} is ready to be paid.";
                OnPropertyChanged(nameof(CurrentOrderLabel));
            }
        }
    }

    public string CurrentOrderLabel => CurrentOrder is null
        ? "No order selected"
        : $"Order #{CurrentOrder.Id} • {CurrentOrder.Status} • {CurrentOrder.TotalPriceLabel}";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand PayCashCommand { get; }
    public ICommand PayCardCommand { get; }

    private async Task PayAsync(string method)
    {
        if (IsBusy)
            return;

        if (CurrentOrder is null)
        {
            StatusMessage = "No order is open.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Processing payment...";

            var payment = await _apiService.PayOrderAsync(CurrentOrder.Id, new PayOrderRequest
            {
                PaymentMethod = method
            });

            CurrentOrder.Status = payment.OrderStatus;
            OnPropertyChanged(nameof(CurrentOrderLabel));
            StatusMessage = $"Payment successful with {method}.";

            await Shell.Current.DisplayAlert("Success", $"Order #{CurrentOrder.Id} was paid by {method}.", "OK");
            await Shell.Current.GoToAsync("//tables");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            await Shell.Current.DisplayAlert("Payment error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            (PayCashCommand as Command)?.ChangeCanExecute();
            (PayCardCommand as Command)?.ChangeCanExecute();
        }
    }
}

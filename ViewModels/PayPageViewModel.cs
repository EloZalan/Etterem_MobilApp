using System.Windows.Input;
using WaiterApp.Models;
using WaiterApp.Services;
using WaiterApp.Services.Payments;

namespace WaiterApp.ViewModels;

public class PayPageViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly INfcPaymentService _nfcPaymentService;
    private Order? _currentOrder;
    private string _statusMessage = "Choose a payment method.";
    private string _selectedPaymentMethod = string.Empty;

    public PayPageViewModel(IApiService apiService, INfcPaymentService nfcPaymentService)
    {
        _apiService = apiService;
        _nfcPaymentService = nfcPaymentService;
        PayCashCommand = new Command(async () => await PayAsync("Készpénzel"), () => !IsBusy);
        PayCardCommand = new Command(async () => await PayByCardAsync(), () => !IsBusy);
    }

    public Order? CurrentOrder
    {
        get => _currentOrder;
        set
        {
            if (SetProperty(ref _currentOrder, value))
            {
                StatusMessage = value is null
                    ? "Nincs kiválasztott rendelés."
                    : $"Rendelés #{value.Id} készen áll a fizetésre.";
                OnPropertyChanged(nameof(CurrentOrderLabel));
            }
        }
    }

    public string CurrentOrderLabel => CurrentOrder is null
        ? "Nincs kiválasztott rendelés"
        : $"Rendelés #{CurrentOrder.Id} • {CurrentOrder.Status} • {CurrentOrder.TotalPriceLabel}";

    public string SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
            if (SetProperty(ref _selectedPaymentMethod, normalized))
            {
                OnPropertyChanged(nameof(IsCashPaymentVisible));
                OnPropertyChanged(nameof(IsCardPaymentVisible));

                StatusMessage = normalized switch
                {
                    "készpénz" => "Készpénzes fizetés megerősítése.",
                    "kártya" => "Használd a telefon NFC szenzorát a fizetéshez.",
                    _ => "Válassz fizetési módot."
                };
            }
        }
    }

    public bool IsCashPaymentVisible => SelectedPaymentMethod == "cash" || string.IsNullOrWhiteSpace(SelectedPaymentMethod);
    public bool IsCardPaymentVisible => SelectedPaymentMethod == "card" || string.IsNullOrWhiteSpace(SelectedPaymentMethod);

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand PayCashCommand { get; }
    public ICommand PayCardCommand { get; }

    private async Task PayByCardAsync()
    {
        if (IsBusy)
            return;

        if (CurrentOrder is null)
        {
            StatusMessage = "No order is open.";
            return;
        }

        if (!_nfcPaymentService.IsSupported)
        {
            StatusMessage = "Az NFC fizetés nem támogatott az eszközön.";
            await Shell.Current.DisplayAlert("NFC not available", StatusMessage, "OK");
            return;
        }

        StatusMessage = "Wárakozás a tranzakcióra...";

        var scanResult = await _nfcPaymentService.WaitForTapAsync();
        if (!scanResult.IsSuccess)
        {
            StatusMessage = scanResult.Message;
            await Shell.Current.DisplayAlert("NFC fizetés", scanResult.Message, "OK");
            return;
        }

        StatusMessage = scanResult.Message;
        await PayAsync("card");
    }

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
            StatusMessage = method == "card" ? "NFC érzékelve. Várakozás a kértyára..." : "Várakozás a tranzakcióra...";

            var payment = await _apiService.PayOrderAsync(CurrentOrder.Id, new PayOrderRequest
            {
                PaymentMethod = method
            });

            CurrentOrder.Status = payment.OrderStatus;
            OnPropertyChanged(nameof(CurrentOrderLabel));
            StatusMessage = method == "card" ? "Sikeres kártyás fizetés." : $"Fizetés sikeres {method}.";

            var successMessage = method == "card"
                ? $"Rendelés #{CurrentOrder.Id} kártyával lett fizetve."
                : $"Rendelsé #{CurrentOrder.Id} {method}-val lett fizetve.";

            await Shell.Current.DisplayAlert("Sikeres", successMessage, "OK");
            await Shell.Current.GoToAsync("//tables");
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            await Shell.Current.DisplayAlert("Fizetés error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            (PayCashCommand as Command)?.ChangeCanExecute();
            (PayCardCommand as Command)?.ChangeCanExecute();
        }
    }
}

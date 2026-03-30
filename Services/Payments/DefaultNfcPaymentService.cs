namespace WaiterApp.Services.Payments;

public class DefaultNfcPaymentService : INfcPaymentService
{
    public bool IsSupported => false;

    public Task<NfcPaymentScanResult> WaitForTapAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new NfcPaymentScanResult
        {
            IsSuccess = false,
            Message = "NFC payment is only available on Android devices with NFC support."
        });
    }
}

namespace WaiterApp.Services.Payments;

public interface INfcPaymentService
{
    bool IsSupported { get; }
    Task<NfcPaymentScanResult> WaitForTapAsync(CancellationToken cancellationToken = default);
}

public class NfcPaymentScanResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? TagId { get; init; }
}

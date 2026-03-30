using MauiApp1;
using Android.App;
using Android.Content;
using Android.Nfc;
using Microsoft.Maui.ApplicationModel;
using WaiterApp.Services.Payments;

namespace WaiterApp.Platforms.Android.Services;

public class AndroidNfcPaymentService : INfcPaymentService
{
    private static AndroidNfcPaymentService? _instance;
    private readonly NfcAdapter? _nfcAdapter;
    private TaskCompletionSource<NfcPaymentScanResult>? _scanTaskSource;
    private Activity? _activeActivity;

    public AndroidNfcPaymentService()
    {
        _nfcAdapter = NfcAdapter.GetDefaultAdapter(global::Android.App.Application.Context);
        _instance = this;
    }

    public bool IsSupported => _nfcAdapter is not null;

    public async Task<NfcPaymentScanResult> WaitForTapAsync(CancellationToken cancellationToken = default)
    {
        if (_nfcAdapter is null)
        {
            return new NfcPaymentScanResult
            {
                IsSuccess = false,
                Message = "This device does not support NFC."
            };
        }

        if (!_nfcAdapter.IsEnabled)
        {
            return new NfcPaymentScanResult
            {
                IsSuccess = false,
                Message = "NFC is turned off on this device. Please enable it and try again."
            };
        }

        var activity = Platform.CurrentActivity ?? MainActivity.Current;
        if (activity is null)
        {
            return new NfcPaymentScanResult
            {
                IsSuccess = false,
                Message = "Could not access the NFC reader on this device."
            };
        }

        if (_scanTaskSource is not null && !_scanTaskSource.Task.IsCompleted)
        {
            return new NfcPaymentScanResult
            {
                IsSuccess = false,
                Message = "An NFC payment is already waiting for a tap."
            };
        }

        _activeActivity = activity;
        _scanTaskSource = new TaskCompletionSource<NfcPaymentScanResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        EnableForegroundDispatch(activity);

        using var registration = cancellationToken.Register(() =>
        {
            CleanupForegroundDispatch();
            _scanTaskSource?.TrySetCanceled(cancellationToken);
        });

        try
        {
            return await _scanTaskSource.Task;
        }
        catch (OperationCanceledException)
        {
            return new NfcPaymentScanResult
            {
                IsSuccess = false,
                Message = "NFC payment was cancelled."
            };
        }
        finally
        {
            CleanupForegroundDispatch();
        }
    }

    public static void ProcessIntent(Intent? intent)
    {
        _instance?.HandleIntent(intent);
    }

    private void HandleIntent(Intent? intent)
    {
        if (intent is null || _scanTaskSource is null || _scanTaskSource.Task.IsCompleted)
            return;

        var action = intent.Action;
        if (action != NfcAdapter.ActionTagDiscovered &&
            action != NfcAdapter.ActionTechDiscovered &&
            action != NfcAdapter.ActionNdefDiscovered)
        {
            return;
        }

        var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
        var tagId = tag?.GetId();
        var readableId = tagId is null || tagId.Length == 0
            ? "unknown"
            : BitConverter.ToString(tagId).Replace("-", string.Empty);

        _scanTaskSource.TrySetResult(new NfcPaymentScanResult
        {
            IsSuccess = true,
            TagId = readableId,
            Message = $"NFC card detected ({readableId})."
        });
    }

    private void EnableForegroundDispatch(Activity activity)
    {
        if (_nfcAdapter is null)
            return;

        var intent = new Intent(activity, activity.Class)
            .AddFlags(ActivityFlags.SingleTop);

        var pendingIntent = PendingIntent.GetActivity(
            activity,
            0,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);

        _nfcAdapter.EnableForegroundDispatch(activity, pendingIntent, Array.Empty<IntentFilter>(), Array.Empty<string[]>());
    }

    private void CleanupForegroundDispatch()
    {
        if (_nfcAdapter is null || _activeActivity is null)
            return;

        try
        {
            _nfcAdapter.DisableForegroundDispatch(_activeActivity);
        }
        catch
        {
        }
        finally
        {
            _activeActivity = null;
        }
    }
}

using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class PayOrderRequest
{
    [JsonPropertyName("payment_method")]
    public string PaymentMethod { get; set; } = "cash";
}

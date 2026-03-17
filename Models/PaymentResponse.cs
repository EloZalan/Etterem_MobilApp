using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class PaymentResponse
{
    [JsonPropertyName("payment_id")]
    public int PaymentId { get; set; }

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = string.Empty;

    public PaymentTable? Table { get; set; }
}

public class PaymentTable
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

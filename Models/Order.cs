using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class Order
{
    public int Id { get; set; }

    [JsonPropertyName("table_id")]
    public int TableId { get; set; }

    [JsonPropertyName("reservation_id")]
    public int? ReservationId { get; set; }

    [JsonPropertyName("waiter_id")]
    public int WaiterId { get; set; }

    [JsonPropertyName("total_price")]
    public int TotalPrice { get; set; }

    public string Status { get; set; } = string.Empty;

    public string TotalPriceLabel => $"{TotalPrice:N0} Ft";
}

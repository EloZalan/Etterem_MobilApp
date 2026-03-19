using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class TableOrderDetails
{
    [JsonPropertyName("order_id")]
    public int? OrderId { get; set; }

    [JsonPropertyName("table_id")]
    public int? TableId { get; set; }

    [JsonPropertyName("reservation_id")]
    public int? ReservationId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("total_price")]
    public int TotalPrice { get; set; }

    [JsonPropertyName("opened_at")]
    public DateTime? OpenedAt { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("items")]
    public List<TableOrderItem> Items { get; set; } = new();

    public bool HasOpenOrder => OrderId.HasValue;
}

public class TableOrderItem
{
    public int Id { get; set; }

    [JsonPropertyName("menu_item_id")]
    public int MenuItemId { get; set; }

    public string? Name { get; set; }
    public int? Price { get; set; }
    public int Quantity { get; set; }

    [JsonPropertyName("line_total")]
    public int? LineTotal { get; set; }

    public string PriceLabel => $"{(Price ?? 0):N0} Ft";
    public string LineTotalLabel => $"{(LineTotal ?? ((Price ?? 0) * Quantity)):N0} Ft";
}
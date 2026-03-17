using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class AddOrderItemRequest
{
    [JsonPropertyName("menu_item_id")]
    public int MenuItemId { get; set; }

    public int Quantity { get; set; }
}

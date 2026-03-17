using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class WaiterMenuItems
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Price { get; set; }

    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    public string PriceLabel => $"{Price:N0} Ft";
}

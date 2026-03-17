namespace WaiterApp.Models;

public class RestaurantTable
{
    public int Id { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;

    // Not present in swagger, kept as optional extension point if backend adds waiter ownership later.
    public int? WaiterId { get; set; }

    public string DisplayName => $"Table {Id}";
    public string Subtitle => $"Capacity: {Capacity} • Status: {Status}";
}

using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class RestaurantTable
{
    public int Id { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;

    public int? WaiterId { get; set; }

    [JsonPropertyName("waiter_name")]
    public string? WaiterName { get; set; }

    [JsonPropertyName("reservation")]
    public Reservation? Reservation { get; set; }

    public bool IsReserved =>
        Reservation is not null ||
        string.Equals(Status, "reserved", StringComparison.OrdinalIgnoreCase);

    public string ReservationText =>
        Reservation is null
            ? "No reservation"
            : $"Reserved for {Reservation.GuestCount} guest(s)";

    public string DisplayName => $"Table {Id}";
    public string Subtitle => $"Capacity: {Capacity} • Status: {Status}";
}
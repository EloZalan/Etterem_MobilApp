using System.Text.Json.Serialization;

namespace WaiterApp.Models;

public class Reservation
{
    public int Id { get; set; }

    [JsonPropertyName("table_id")]
    public int TableId { get; set; }

    [JsonPropertyName("guest_name")]
    public string? GuestName { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("guest_count")]
    public int GuestCount { get; set; }

    [JsonPropertyName("start_time")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("end_time")]
    public DateTime? EndTime { get; set; }
}
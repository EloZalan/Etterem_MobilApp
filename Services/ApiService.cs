using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WaiterApp.Models;

namespace WaiterApp.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    // ApiService.cs – HTTP kliens inicializálása
    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://jcloud02.jedlik.eu/schmitzhofer.pal/backend/api/")
        };

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }
    // Token beállítása bejelentkezés után:
    public void SetToken(string? token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = await PostAsync("login", request, requiresSuccess: false);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new Exception("Invalid email or password.");

        return await ReadAsync<LoginResponse>(response) ?? new LoginResponse();
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync("user");
        return await ReadAsync<User>(response);
    }

    public async Task<List<RestaurantTable>> GetTablesAsync()
    {
        var response = await _httpClient.GetAsync("tables");
        return await ReadAsync<List<RestaurantTable>>(response) ?? new List<RestaurantTable>();
    }

    public async Task<List<MenuCategory>> GetMenuCategoriesAsync()
    {
        var response = await _httpClient.GetAsync("menu-categories");
        return await ReadAsync<List<MenuCategory>>(response) ?? new List<MenuCategory>();
    }

    public async Task<List<WaiterMenuItems>> GetMenuItemsAsync()
    {
        var response = await _httpClient.GetAsync("menu-items");
        return await ReadAsync<List<WaiterMenuItems>>(response) ?? new List<WaiterMenuItems>();
    }

    public async Task<Order> OpenOrderForTableAsync(int tableId)
    {
        var response = await PostAsync($"tables/{tableId}/orders", new { }, requiresSuccess: false);

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception(await ExtractErrorMessageAsync(response,
                "Cannot open order for this table. The table may already have an open order or no valid reservation exists."));

        return await ReadAsync<Order>(response) ?? throw new Exception("Order response was empty.");
    }

    public async Task AddOrderItemAsync(int orderId, AddOrderItemRequest request)
    {
        var response = await PostAsync($"orders/{orderId}/items", request, requiresSuccess: false);

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception(await ExtractErrorMessageAsync(response, "The order cannot be modified anymore."));

    }

    public async Task DeleteOrderItemAsync(int orderId, int orderItemId)
    {
        var candidates = new HttpRequestMessage[]
        {
            new(HttpMethod.Delete, $"orders/{orderId}/items/{orderItemId}"),
            new(HttpMethod.Delete, $"order-items/{orderItemId}"),
            new(HttpMethod.Post, $"orders/{orderId}/items/{orderItemId}/delete")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            },
            new(HttpMethod.Post, $"order-items/{orderItemId}/delete")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }
        };

        string? lastBody = null;
        HttpStatusCode? lastStatus = null;

        foreach (var request in candidates)
        {
            using var response = await _httpClient.SendAsync(request);
            lastStatus = response.StatusCode;

            if (response.IsSuccessStatusCode)
                return;

            lastBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.MethodNotAllowed)
                throw new Exception(await ExtractErrorMessageAsync(response, "Could not delete item from order."));
        }

        throw new Exception(string.IsNullOrWhiteSpace(lastBody)
            ? $"Could not delete item from order. Last status: {lastStatus}."
            : lastBody);
    }

    public async Task<TableOrderDetails?> GetCurrentOrderForTableAsync(int tableId)
    {
        var response = await _httpClient.GetAsync($"tables/{tableId}/orders");

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return null;

        return await ReadAsync<TableOrderDetails>(response);
    }

    public async Task SimulateReadyAsync(int orderId)
    {
        var response = await PostAsync($"orders/{orderId}/simulate-ready", new { }, requiresSuccess: false);

        if ((int)response.StatusCode == 422)
            throw new Exception(await ExtractErrorMessageAsync(response, "No payable order found."));

    }

    public async Task<PaymentResponse> PayOrderAsync(int orderId, PayOrderRequest request)
    {
        var response = await PostAsync($"orders/{orderId}/pay", request, requiresSuccess: false);

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception(await ExtractErrorMessageAsync(response, "Order status is not valid for payment."));

        return await ReadAsync<PaymentResponse>(response) ?? new PaymentResponse();
    }

    public async Task DropShiftAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "end-shift");
        var response = await _httpClient.SendAsync(request);

        Console.WriteLine($"END SHIFT STATUS: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"END SHIFT BODY: {await response.Content.ReadAsStringAsync()}");
    }

    public async Task TakeShiftAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "take-shift");
        var response = await _httpClient.SendAsync(request);

        Console.WriteLine($"TAKE SHIFT STATUS: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"TAKE SHIFT BODY: {await response.Content.ReadAsStringAsync()}");
    }

    public async Task LogoutAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "logout");
        var response = await _httpClient.SendAsync(request);

        Console.WriteLine($"LOGOUT STATUS: {(int)response.StatusCode} {response.StatusCode}");
        Console.WriteLine($"LOGOUT BODY: {await response.Content.ReadAsStringAsync()}");

    }

    private async Task<HttpResponseMessage> PostAsync<T>(string uri, T data, bool requiresSuccess = true)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(uri, content);

        if (requiresSuccess)
            await EnsureSuccessWithMessage(response, $"Request to '{uri}' failed.");

        return response;
    }

    private async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
    }

    private static async Task EnsureSuccessWithMessage(HttpResponseMessage response, string fallbackMessage)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await ExtractErrorMessageAsync(response, fallbackMessage);
        throw new Exception(message);
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, string fallbackMessage)
    {
        var raw = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(raw))
            return fallbackMessage;

        try
        {
            var apiError = JsonSerializer.Deserialize<ApiError>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!string.IsNullOrWhiteSpace(apiError?.Message))
                return apiError.Message;
        }
        catch
        {
        }

        return raw;
    }
    public async Task<Reservation> CreateWalkInReservationAsync(int guestCount)
    {
        var response = await PostAsync("reservations/walk-in", new
        {
            guest_count = guestCount
        }, requiresSuccess: false);

        if ((int)response.StatusCode == 422)
            throw new Exception("No free table is available right now.");
        return await ReadAsync<Reservation>(response)
               ?? throw new Exception("Walk-in reservation response was empty.");
    }
}
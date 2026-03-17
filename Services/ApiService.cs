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

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://10.0.2.2:8000/api/")
        };
    }

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

        await EnsureSuccessWithMessage(response, "Login failed.");
        return await ReadAsync<LoginResponse>(response) ?? new LoginResponse();
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var response = await _httpClient.GetAsync("user");
        await EnsureSuccessWithMessage(response, "Could not load current user.");
        return await ReadAsync<User>(response);
    }

    public async Task<List<RestaurantTable>> GetTablesAsync()
    {
        var response = await _httpClient.GetAsync("tables");
        await EnsureSuccessWithMessage(response, "Could not load tables.");
        return await ReadAsync<List<RestaurantTable>>(response) ?? new List<RestaurantTable>();
    }

    public async Task<List<MenuCategory>> GetMenuCategoriesAsync()
    {
        var response = await _httpClient.GetAsync("menu-categories");
        await EnsureSuccessWithMessage(response, "Could not load menu categories.");
        return await ReadAsync<List<MenuCategory>>(response) ?? new List<MenuCategory>();
    }

    public async Task<List<WaiterMenuItems>> GetMenuItemsAsync()
    {
        var response = await _httpClient.GetAsync("menu-items");
        await EnsureSuccessWithMessage(response, "Could not load menu items.");
        return await ReadAsync<List<WaiterMenuItems>>(response) ?? new List<WaiterMenuItems>();
    }

    public async Task<Order> OpenOrderForTableAsync(int tableId)
    {
        var response = await PostAsync($"tables/{tableId}/orders", new { }, requiresSuccess: false);
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var raw = await response.Content.ReadAsStringAsync();
            throw new Exception(string.IsNullOrWhiteSpace(raw)
                ? "Cannot open order for this table. The table may already have an open order or no valid reservation exists."
                : raw);
        }

        await EnsureSuccessWithMessage(response, "Could not open order.");
        return await ReadAsync<Order>(response) ?? throw new Exception("Order response was empty.");
    }

    public async Task AddOrderItemAsync(int orderId, AddOrderItemRequest request)
    {
        var response = await PostAsync($"orders/{orderId}/items", request, requiresSuccess: false);
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception("The order cannot be modified anymore.");

        await EnsureSuccessWithMessage(response, "Could not add item to order.");
    }

    public async Task SimulateReadyAsync(int orderId)
    {
        var response = await PostAsync($"orders/{orderId}/simulate-ready", new { }, requiresSuccess: false);
        if ((int)response.StatusCode == 422)
            throw new Exception("No payable order found.");

        await EnsureSuccessWithMessage(response, "Could not set order to ready-to-pay.");
    }

    public async Task<PaymentResponse> PayOrderAsync(int orderId, PayOrderRequest request)
    {
        var response = await PostAsync($"orders/{orderId}/pay", request, requiresSuccess: false);
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception("Order status is not valid for payment.");

        await EnsureSuccessWithMessage(response, "Payment failed.");
        return await ReadAsync<PaymentResponse>(response) ?? new PaymentResponse();
    }

    private async Task<HttpResponseMessage> PostAsync<T>(string uri, T data, bool requiresSuccess = true)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var response = await _httpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
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

        var raw = await response.Content.ReadAsStringAsync();
        throw new Exception(string.IsNullOrWhiteSpace(raw) ? fallbackMessage : raw);
    }
}

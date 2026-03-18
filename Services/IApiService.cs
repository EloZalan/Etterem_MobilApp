using WaiterApp.Models;

namespace WaiterApp.Services;

public interface IApiService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<User?> GetCurrentUserAsync();
    Task<List<RestaurantTable>> GetTablesAsync();
    Task<List<MenuCategory>> GetMenuCategoriesAsync();
    Task<List<WaiterMenuItems>> GetMenuItemsAsync();
    Task<Order> OpenOrderForTableAsync(int tableId);
    Task<TableOrderDetails?> GetCurrentOrderForTableAsync(int tableId);
    Task AddOrderItemAsync(int orderId, AddOrderItemRequest request);
    Task SimulateReadyAsync(int orderId);
    Task<PaymentResponse> PayOrderAsync(int orderId, PayOrderRequest request);
    Task<Reservation> CreateWalkInReservationAsync(int guestCount);
    Task DropShiftAsync();
    Task TakeShiftAsync();
    Task LogoutAsync();
    void SetToken(string? token);
}
using WaiterApp.Models;

namespace WaiterApp.Services;

public interface IAuthService
{
    User? CurrentUser { get; }
    string? Token { get; }
    Task InitializeAsync();
    Task LoginAsync(LoginResponse response);
    Task LogoutAsync();
}

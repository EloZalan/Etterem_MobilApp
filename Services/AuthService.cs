using WaiterApp.Models;

namespace WaiterApp.Services;

public class AuthService : IAuthService
{
    private const string TokenKey = "auth_token";
    private readonly IApiService _apiService;

    public User? CurrentUser { get; private set; }
    public string? Token { get; private set; }

    public AuthService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task InitializeAsync()
    {
        Token = await SecureStorage.Default.GetAsync(TokenKey);
        _apiService.SetToken(Token);

        if (string.IsNullOrWhiteSpace(Token))
            return;

        try
        {
            CurrentUser = await _apiService.GetCurrentUserAsync();
        }
        catch
        {
            await LogoutAsync();
        }
    }

    public async Task LoginAsync(LoginResponse response)
    {
        Token = response.token;
        CurrentUser = response.user;
        _apiService.SetToken(Token);

        if (!string.IsNullOrWhiteSpace(Token))
            await SecureStorage.Default.SetAsync(TokenKey, Token);
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiService.LogoutAsync();
        }
        catch
        {
            
        }

        Token = null;
        CurrentUser = null;
        _apiService.SetToken(null);
        SecureStorage.Default.Remove(TokenKey);
        await Task.CompletedTask;
    }
}

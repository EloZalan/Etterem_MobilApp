using System.Windows.Input;
using WaiterApp.Models;
using WaiterApp.Services;

namespace WaiterApp.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly IAuthService _authService;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(IApiService apiService, IAuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
        Title = "Login";
        LoginCommand = new Command(async () => await LoginAsync(), () => !IsBusy);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand LoginCommand { get; }

    public async Task CheckExistingSessionAsync()
    {
        if (_authService.CurrentUser is not null)
            await Shell.Current.GoToAsync("//tables");
    }

    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var response = await _apiService.LoginAsync(new LoginRequest
            {
                email = Email,
                password = Password
            });

            if (response.user?.Role != "waiter")
                throw new Exception("This mobile app is for waiters only.");

            await _authService.LoginAsync(response);
            await Shell.Current.GoToAsync("//tables");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

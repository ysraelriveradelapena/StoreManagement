
using StoreManagement.Web.DTOs.Auth;

namespace StoreManagement.Web.Services;

public class AuthState
{
    private readonly AuthService _authService;

    public AuthState(AuthService authService)
    {
        _authService = authService;
    }

    public bool IsLoggedIn => _authService.IsLoggedIn;
    public LoginUser? CurrentUser => _authService.CurrentUser;
    public string? Token => _authService.Token;

    public async Task InitializeAsync()
    {
        await _authService.InitializeAsync();
        NotifyAuthenticationStateChanged();
    }

    public event Action? AuthenticationStateChanged;

    public void NotifyAuthenticationStateChanged()
    {
        AuthenticationStateChanged?.Invoke();
    }
}

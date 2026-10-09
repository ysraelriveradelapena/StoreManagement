
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using StoreManagement.Web.DTOs.Auth;
using System.Net.Http.Json;

namespace StoreManagement.Web.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProtectedSessionStorage _sessionStorage;

    private bool _initialized;

    public string? Token { get; private set; }

    public bool IsLoggedIn =>
        !string.IsNullOrWhiteSpace(Token);

    public LoginUser? CurrentUser { get; private set; }

    public AuthService(
        IHttpClientFactory httpClientFactory,
        ProtectedSessionStorage sessionStorage)
    {
        _httpClientFactory = httpClientFactory;
        _sessionStorage = sessionStorage;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        var tokenResult =
            await _sessionStorage.GetAsync<string>("store_access_token");

        var userResult =
            await _sessionStorage.GetAsync<LoginUser>("store_current_user");

        Token = tokenResult.Success ? tokenResult.Value : null;
        CurrentUser = userResult.Success ? userResult.Value : null;

        _initialized = true;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var httpClient = _httpClientFactory.CreateClient("AuthApi");

        var request = new { email, password };

        var response = await httpClient.PostAsJsonAsync(
            "api/auth/login", request);

        if (!response.IsSuccessStatusCode)
            return false;

        var result =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (result == null ||
            string.IsNullOrWhiteSpace(result.AccessToken))
        {
            return false;
        }

        Token = result.AccessToken;
        CurrentUser = result.User;
        _initialized = true;

        await _sessionStorage.SetAsync(
            "store_access_token", Token);

        await _sessionStorage.SetAsync(
            "store_current_user", CurrentUser);

        return true;
    }

    public async Task LogoutAsync()
    {
        Token = null;
        CurrentUser = null;
        _initialized = true;

        await _sessionStorage.DeleteAsync("store_access_token");
        await _sessionStorage.DeleteAsync("store_current_user");
    }
}

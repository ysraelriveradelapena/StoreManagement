
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace StoreManagement.Web.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public ApiService(
        IHttpClientFactory httpClientFactory,
        AuthService authService)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
        _authService = authService;
    }

    private async Task AddAuthorizationHeaderAsync()
    {
        await _authService.InitializeAsync();

        _httpClient.DefaultRequestHeaders.Authorization = null;

        if (!string.IsNullOrWhiteSpace(_authService.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer", _authService.Token);
        }
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        await AddAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<T>(url);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(
        string url, T data)
    {
        await AddAuthorizationHeaderAsync();
        return await _httpClient.PostAsJsonAsync(url, data);
    }

    public async Task<HttpResponseMessage> PatchAsync<T>(
    string url,
    T data)
    {
        await AddAuthorizationHeaderAsync();

        return await _httpClient.PatchAsJsonAsync(url, data);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(
    string url,
    T data)
    {
        await AddAuthorizationHeaderAsync();

        return await _httpClient.PutAsJsonAsync(url, data);
    }
}

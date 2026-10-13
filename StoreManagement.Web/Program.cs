
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using StoreManagement.Web.Components;
using StoreManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];

if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "ApiBaseUrl is missing from configuration.");
}

builder.Services.AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "StoreManagement.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<ApiService>();

builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddScoped<
    Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

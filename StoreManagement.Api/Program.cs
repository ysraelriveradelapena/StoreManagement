
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StoreManagement.Api.Data;
using StoreManagement.Api.Models.Identity;
using StoreManagement.Api.Services.Authentication;
using StoreManagement.Api.Services.Inventory;
using StoreManagement.Api.Services.Pricing;
using StoreManagement.Api.Services.Sales;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// ------------------------------------------------------------
// DATABASE
// ------------------------------------------------------------

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

// ------------------------------------------------------------
// IDENTITY
// ------------------------------------------------------------

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

// ------------------------------------------------------------
// JWT AUTHENTICATION
// ------------------------------------------------------------

var jwt = configuration.GetSection("Jwt");

var jwtKey = jwt["Key"]
    ?? throw new InvalidOperationException(
        "JWT key is not configured.");

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT key must be at least 32 characters.");
}

var jwtIssuer = jwt["Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = jwt["Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

var deviceJwtKey = jwt["DeviceKey"]
    ?? throw new InvalidOperationException(
        "JWT device key is not configured.");

if (deviceJwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT device key must be at least 32 characters.");
}

var deviceJwtIssuer =
    jwt["DeviceIssuer"] ?? "StoreManagement.Device";

var deviceJwtAudience =
    jwt["DeviceAudience"] ?? "StoreManagement.DeviceClients";

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.FromMinutes(1),
                RoleClaimType = "role"
            };
    })
    .AddJwtBearer("DeviceBearer", options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = deviceJwtIssuer,
                ValidAudience = deviceJwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(deviceJwtKey)),

                ClockSkew = TimeSpan.FromMinutes(1)
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var deviceIdValue = context.Principal?
                    .FindFirst("device_id")?.Value;

                var deviceAccess = context.Principal?
                    .FindFirst("device_access")?.Value;

                if (deviceAccess != "true" ||
                    !int.TryParse(
                        deviceIdValue,
                        out var deviceId))
                {
                    context.Fail("Invalid device token.");
                    return;
                }

                using var scope = context.HttpContext
                    .RequestServices.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var device = await db.Devices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == deviceId);

                if (device == null ||
                    !device.IsActive ||
                    string.IsNullOrWhiteSpace(
                        device.CredentialHash))
                {
                    context.Fail(
                        "This device is inactive or no longer registered.");
                }
            }
        };
    });

// ------------------------------------------------------------
// AUTHORIZATION
// ------------------------------------------------------------

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveDevice", policy =>
    {
        policy.AddAuthenticationSchemes("DeviceBearer");
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("device_access", "true");
    });
});

// ------------------------------------------------------------
// APPLICATION SERVICES
// ------------------------------------------------------------

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<ProductVariantPriceService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<SalesService>();

// ------------------------------------------------------------
// CONTROLLERS
// ------------------------------------------------------------

builder.Services.AddControllers();

// ------------------------------------------------------------
// SWAGGER
// ------------------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Store Management API",
            Version = "v1"
        });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {your JWT token}"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

// ------------------------------------------------------------
// CORS
// ------------------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApp", policy =>
    {
        policy
            .WithOrigins(
                "https://localhost:7001",
                "https://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ------------------------------------------------------------
// BUILD
// ------------------------------------------------------------

var app = builder.Build();

// ------------------------------------------------------------
// DATABASE SEEDING
// ------------------------------------------------------------

await DbSeeder.SeedAsync(app.Services);

// ------------------------------------------------------------
// HTTP PIPELINE
// ------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("WebApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

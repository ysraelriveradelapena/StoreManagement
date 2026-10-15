
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Models.Identity;

namespace StoreManagement.Api.Data;

public static class DbSeeder
{
    public const string SystemAdminRole = "SystemAdmin";
    public const string StoreBARole = "StoreBA";
    public const string WarehouseRole = "Warehouse";
    public const string HeadOfficeSalesRole = "HeadOfficeSales";
    public const string HeadOfficeInventoryRole = "HeadOfficeInventory";
    public const string AccountingRole = "Accounting";
    public const string ManagementRole = "Management";

    public static readonly string[] Roles =
    {
        SystemAdminRole,
        StoreBARole,
        WarehouseRole,
        HeadOfficeSalesRole,
        HeadOfficeInventoryRole,
        AccountingRole,
        ManagementRole
    };

    public static async Task SeedAsync(
        IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var provider = scope.ServiceProvider;

        var db = provider
            .GetRequiredService<ApplicationDbContext>();

        var roleManager = provider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = provider
            .GetRequiredService<UserManager<ApplicationUser>>();

        // Development only.
        // Production migrations must be applied separately.
        if (provider.GetRequiredService<IHostEnvironment>()
            .IsDevelopment())
        {
            await db.Database.MigrateAsync();
        }

        // Verify that production database is initialized.
        if (!await db.Database.CanConnectAsync())
        {
            throw new InvalidOperationException(
                "Unable to connect to StoreManagement database.");
        }

        if ((await db.Database.GetPendingMigrationsAsync()).Any())
        {
            throw new InvalidOperationException(
                "Pending database migrations detected. Apply migrations before starting the API.");
        }

        await SeedRolesAsync(roleManager);

        await SeedAdministratorAsync(
            userManager,
            provider);
    }

    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create role '{role}': " +
                    string.Join("; ",
                        result.Errors.Select(x => x.Description)));
            }
        }
    }

    private static async Task SeedAdministratorAsync(
        UserManager<ApplicationUser> userManager,
        IServiceProvider services)
    {
        var configuration = services
            .GetRequiredService<IConfiguration>();

        var email = configuration["AdminUser:Email"];
        var password = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "AdminUser:Email is not configured.");
        }

        var existingUser =
            await userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            if (!await userManager.IsInRoleAsync(
                existingUser, SystemAdminRole))
            {
                var result = await userManager.AddToRoleAsync(
                    existingUser, SystemAdminRole);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to assign administrator role: " +
                        string.Join("; ",
                            result.Errors.Select(x => x.Description)));
                }
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "AdminUser:Password is required when creating the initial administrator.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(
            user, password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to create administrator: " +
                string.Join("; ",
                    createResult.Errors.Select(x => x.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(
            user, SystemAdminRole);

        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to assign administrator role: " +
                string.Join("; ",
                    roleResult.Errors.Select(x => x.Description)));
        }
    }
}

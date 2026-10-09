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
        using var scope =
            services.CreateScope();

        var serviceProvider =
            scope.ServiceProvider;

        var db =
            serviceProvider
                .GetRequiredService<ApplicationDbContext>();

        var roleManager =
            serviceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager =
            serviceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);

        await SeedAdministratorAsync(
            userManager,
            serviceProvider);
    }

    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            if (!result.Succeeded)
            {
                var errors =
                    string.Join(
                        "; ",
                        result.Errors.Select(
                            x => x.Description));

                throw new InvalidOperationException(
                    $"Unable to create role '{role}': {errors}");
            }
        }
    }

    private static async Task SeedAdministratorAsync(
        UserManager<ApplicationUser> userManager,
        IServiceProvider services)
    {
        var configuration =
            services
                .GetRequiredService<IConfiguration>();

        var email =
            configuration["AdminUser:Email"];

        var password =
            configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "AdminUser:Email is not configured.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "AdminUser:Password is not configured.");
        }

        var existingUser =
            await userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            if (!await userManager.IsInRoleAsync(
                    existingUser,
                    SystemAdminRole))
            {
                await userManager.AddToRoleAsync(
                    existingUser,
                    SystemAdminRole);
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,

            Email = email,

            EmailConfirmed = true,

            IsActive = true,

            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult =
            await userManager.CreateAsync(
                user,
                password);

        if (!createResult.Succeeded)
        {
            var errors =
                string.Join(
                    "; ",
                    createResult.Errors.Select(
                        x => x.Description));

            throw new InvalidOperationException(
                $"Unable to create administrator: {errors}");
        }

        var roleResult =
            await userManager.AddToRoleAsync(
                user,
                SystemAdminRole);

        if (!roleResult.Succeeded)
        {
            var errors =
                string.Join(
                    "; ",
                    roleResult.Errors.Select(
                        x => x.Description));

            throw new InvalidOperationException(
                $"Unable to assign administrator role: {errors}");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace StoreManagement.Api.Data
{
    public class ApplicationDbContextFactory
        : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                basePath = Path.Combine(
                    basePath,
                    "StoreManagement.Api");
            }

            if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                throw new FileNotFoundException(
                    "Cannot locate StoreManagement.Api/appsettings.json");
            }

            // Design-time configuration for local EF Core tooling.
            // Never hardcode the production database credentials here.
            var environment =
                Environment.GetEnvironmentVariable(
                    "ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(environment))
            {
                environment = "Development";
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false)
                .AddJsonFile(
                    $"appsettings.{environment}.json",
                    optional: true)
                .AddUserSecrets<ApplicationDbContextFactory>(
                    optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString =
                configuration.GetConnectionString(
                    "DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"DefaultConnection is missing for environment '{environment}'. " +
                    $"Check appsettings.{environment}.json or User Secrets.");
            }

            var optionsBuilder =
                new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });

            return new ApplicationDbContext(
                optionsBuilder.Options);
        }
    }
}

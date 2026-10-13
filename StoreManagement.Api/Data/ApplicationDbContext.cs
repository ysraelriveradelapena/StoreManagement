using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Models;
using StoreManagement.Api.Models.Identity;
using System.Reflection.Emit;

namespace StoreManagement.Api.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductVariantPrice> ProductVariantPrices => Set<ProductVariantPrice>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<TransactionLine> TransactionLines => Set<TransactionLine>();

    public DbSet<InventoryTransaction> InventoryTransactions
        => Set<InventoryTransaction>();

    public DbSet<TransactionType> TransactionTypes
        => Set<TransactionType>();

    public DbSet<ReasonCode> ReasonCodes
        => Set<ReasonCode>();

    public DbSet<AuditLog> AuditLogs
        => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens
        => Set<RefreshToken>();

    public DbSet<DeviceRegistrationRequest> DeviceRegistrationRequests
    => Set<DeviceRegistrationRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.SKU)
                .IsUnique();

            entity.HasIndex(x => x.Barcode)
                .IsUnique()
                .HasFilter("[Barcode] IS NOT NULL");

            entity.Property(x => x.SKU)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Barcode)
                .HasMaxLength(100);

            entity.Property(x => x.VariantName)
                .HasMaxLength(100);

            entity.Property(x => x.Size)
                .HasMaxLength(50);

            entity.Property(x => x.Color)
                .HasMaxLength(50);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.Variants)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Store>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.StoreCode)
                .IsUnique();

            entity.Property(x => x.StoreCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.StoreName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.DepartmentStore)
                .HasMaxLength(100);

            entity.Property(x => x.Location)
                .HasMaxLength(100);
        });

        builder.Entity<Device>()
            .HasIndex(x => x.Code)
            .IsUnique();

        builder.Entity<Transaction>()
            .HasIndex(x => x.TransactionId)
            .IsUnique();

        builder.Entity<Transaction>()
            .HasIndex(x => new
            {
                x.StoreId,
                x.TransactionDateUtc
            });

        builder.Entity<InventoryTransaction>()
    .HasIndex(x => new
    {
        x.StoreId,
        x.ProductVariantId,
        x.CreatedAtUtc
    });

        builder.Entity<InventoryTransaction>()
            .HasIndex(x => new
            {
                x.StoreId,
                x.CreatedAtUtc
            });

        builder.Entity<AuditLog>()
            .HasIndex(x => new
            {
                x.EntityName,
                x.EntityId
            });

        builder.Entity<RefreshToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Entity<RefreshToken>()
            .HasIndex(x => new
            {
                x.UserId,
                x.ExpiresAtUtc
            });

        builder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(x => x.Store)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Brand>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.ProductCode)
                .IsUnique();

            entity.Property(x => x.ProductCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(x => x.Brand)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ProductVariantPrice>()
        .HasOne(x => x.ProductVariant)
        .WithMany(x => x.Prices)
        .HasForeignKey(x => x.ProductVariantId)
        .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProductVariantPrice>()
            .Property(x => x.Price)
            .HasColumnType("decimal(18,2)");

        builder.Entity<ProductVariantPrice>()
            .Property(x => x.PriceType)
            .HasMaxLength(30)
            .IsRequired();
        builder.Entity<Device>(entity =>
        {
            entity.Property(x => x.Code)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.CredentialHash)
                .HasMaxLength(64);

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });

        builder.Entity<DeviceRegistrationRequest>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.ReviewNotes)
                .HasMaxLength(500);

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.RequestedByUser)
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.Status,
                x.RequestedAtUtc
            });
        });
    }

}
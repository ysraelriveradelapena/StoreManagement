
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Api.Models;
using StoreManagement.Api.Models.Identity;

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
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();
    public DbSet<ReasonCode> ReasonCodes => Set<ReasonCode>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DeviceRegistrationRequest> DeviceRegistrationRequests
        => Set<DeviceRegistrationRequest>();
    public DbSet<StoreDayStatus> StoreDayStatuses => Set<StoreDayStatus>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Store
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

        // Application users
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(x => x.Store)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Devices
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

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Device registration requests
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

        // Brands
        builder.Entity<Brand>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();
        });

        // Categories
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Name)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();
        });

        // Products
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

        // Product variants
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

        // Product variant prices
        builder.Entity<ProductVariantPrice>(entity =>
        {
            entity.HasOne(x => x.ProductVariant)
                .WithMany(x => x.Prices)
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.PriceType)
                .HasMaxLength(30)
                .IsRequired();
        });

        // Transaction types
        builder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });

        // Reason codes
        builder.Entity<ReasonCode>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(x => x.Code)
                .IsUnique();
        });

        // Transaction headers
        builder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TransactionId)
                .IsRequired();

            entity.HasIndex(x => x.TransactionId)
                .IsUnique();

            entity.Property(x => x.TransactionDateUtc)
                .IsRequired();

            entity.Property(x => x.ReceivedAtUtc)
                .IsRequired();

            entity.Property(x => x.ReferenceNumber)
                .HasMaxLength(100);

            entity.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Remarks)
                .HasMaxLength(1000);

            entity.HasIndex(x => new
            {
                x.StoreId,
                x.TransactionDateUtc
            });

            entity.HasIndex(x => x.CountSessionId);

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Device)
                .WithMany()
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.TransactionType)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.TransactionTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Transaction detail lines
        builder.Entity<TransactionLine>(entity =>
        {
            entity.Property(x => x.Barcode)
                .HasMaxLength(100);

            entity.Property(x => x.LocationCode)
                .HasMaxLength(50);

            entity.Property(x => x.ReasonCode)
                .HasMaxLength(50);

            entity.Property(x => x.Remarks)
                .HasMaxLength(500);

            entity.Property(x => x.Quantity)
                .HasColumnType("decimal(18,4)");

            entity.Property(x => x.UnitPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            entity.HasIndex(x => x.TransactionId);

            entity.HasOne(x => x.Transaction)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ProductVariant)
                .WithMany()
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Inventory transactions
        builder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasIndex(x => new
            {
                x.StoreId,
                x.ProductVariantId,
                x.CreatedAtUtc
            });

            entity.HasIndex(x => new
            {
                x.StoreId,
                x.CreatedAtUtc
            });
        });

        // Audit logs
        builder.Entity<AuditLog>()
            .HasIndex(x => new
            {
                x.EntityName,
                x.EntityId
            });

        // Refresh tokens
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.TokenHash)
                .IsUnique();

            entity.HasIndex(x => new
            {
                x.UserId,
                x.ExpiresAtUtc
            });

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // One daily status per store and business date
        builder.Entity<StoreDayStatus>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Remarks)
                .HasMaxLength(500);

            entity.Property(x => x.SubmittedByUserId)
                .HasMaxLength(450);

            entity.Property(x => x.BusinessDate)
                .HasColumnType("date");

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.StoreId,
                x.BusinessDate
            })
            .IsUnique();
        });
    }
}

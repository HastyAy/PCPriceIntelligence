using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // PC Price Intelligence entities
    public DbSet<Component> Components => Set<Component>();
    public DbSet<Price> Prices => Set<Price>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<CompatibilityRule> CompatibilityRules => Set<CompatibilityRule>();
    public DbSet<PCBuild> PCBuilds => Set<PCBuild>();
    public DbSet<SearchQuery> SearchQueries => Set<SearchQuery>();
    public DbSet<ScrapingJob> ScrapingJobs => Set<ScrapingJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // IMPORTANT: Keep Identity tables

        // Component configuration
        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AveragePrice).HasPrecision(18, 2);
            entity.Property(e => e.LowestPrice).HasPrecision(18, 2);

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Manufacturer);
            entity.HasIndex(e => e.EAN);
            entity.HasIndex(e => e.PartNumber);
        });

        // Price configuration
        modelBuilder.Entity<Price>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("EUR");

            entity.HasIndex(e => e.ComponentId);
            entity.HasIndex(e => e.Retailer);
            entity.HasIndex(e => e.ScrapedAt);

            entity.HasOne(e => e.Component)
                .WithMany(c => c.Prices)
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PriceHistory configuration
        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2).IsRequired();

            entity.HasIndex(e => e.ComponentId);
            entity.HasIndex(e => e.RecordedAt);

            entity.HasOne(e => e.Component)
                .WithMany(c => c.PriceHistories)
                .HasForeignKey(e => e.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CompatibilityRule configuration
        modelBuilder.Entity<CompatibilityRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).IsRequired().HasMaxLength(500);

            entity.HasIndex(e => new { e.SourceType, e.TargetType });
        });

        // PCBuild configuration
        modelBuilder.Entity<PCBuild>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsPublic);
        });

        // SearchQuery configuration
        modelBuilder.Entity<SearchQuery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Query).IsRequired().HasMaxLength(500);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SearchedAt);
        });

        // ScrapingJob configuration
        modelBuilder.Entity<ScrapingJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Retailer);
        });
    }
}
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
    public DbSet<PCBuild> PCBuilds => Set<PCBuild>();

    // Specification entities
    public DbSet<CPUSpecification> CPUSpecifications => Set<CPUSpecification>();
    public DbSet<GPUSpecification> GPUSpecifications => Set<GPUSpecification>();
    public DbSet<PSUSpecification> PSUSpecifications => Set<PSUSpecification>();
    public DbSet<RAMSpecification> RAMSpecifications => Set<RAMSpecification>();
    public DbSet<StorageSpecification> StorageSpecifications => Set<StorageSpecification>();
    public DbSet<MotherboardSpec> MotherboardSpecs => Set<MotherboardSpec>();
    public DbSet<CPUCoolerSpecification> CPUCoolerSpecifications => Set<CPUCoolerSpecification>();
    public DbSet<CaseSpecification> CaseSpecifications => Set<CaseSpecification>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Component configuration
        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AveragePrice).HasPrecision(18, 2);
            entity.Property(e => e.LowestPrice).HasPrecision(18, 2);

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Manufacturer);

            // One-to-one relationships with specifications
            entity.HasOne(e => e.CPUSpec)
                .WithOne(s => s.Component)
                .HasForeignKey<CPUSpecification>(s => s.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.GPUSpec)
                .WithOne(s => s.Component)
                .HasForeignKey<GPUSpecification>(s => s.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PSUSpec)
                .WithOne(s => s.Component)
                .HasForeignKey<PSUSpecification>(s => s.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RAMSpec)
                .WithOne(s => s.Component)
                .HasForeignKey<RAMSpecification>(s => s.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StorageSpec)
                .WithOne(s => s.Component)
                .HasForeignKey<StorageSpecification>(s => s.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MotherboardSpec)
                .WithOne(s => s.Component)
                .HasForeignKey<MotherboardSpec>(s => s.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
            // CPU Cooler
            modelBuilder.Entity<CPUCoolerSpecification>()
                .HasOne(c => c.Component)
                .WithOne(c => c.CPUCoolerSpec)
                .HasForeignKey<CPUCoolerSpecification>(c => c.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Case
            modelBuilder.Entity<CaseSpecification>()
                .HasOne(c => c.Component)
                .WithOne(c => c.CaseSpec)
                .HasForeignKey<CaseSpecification>(c => c.ComponentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CPUSpecification configuration
        modelBuilder.Entity<CPUSpecification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ComponentId).IsUnique();
            entity.Property(e => e.BaseClock).HasPrecision(5, 2);
            entity.Property(e => e.BoostClock).HasPrecision(5, 2);
        });

        // GPUSpecification configuration
        modelBuilder.Entity<GPUSpecification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ComponentId).IsUnique();
        });

        // PSUSpecification configuration
        modelBuilder.Entity<PSUSpecification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ComponentId).IsUnique();
        });

        // RAMSpecification configuration
        modelBuilder.Entity<RAMSpecification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ComponentId).IsUnique();
        });

        // StorageSpecification configuration
        modelBuilder.Entity<StorageSpecification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ComponentId).IsUnique();
        });
        modelBuilder.Entity<MotherboardSpec>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ComponentId).IsUnique();
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
        modelBuilder.Entity<PCBuild>()
       .HasOne<ApplicationUser>()
       .WithMany(u => u.PCBuilds)
       .HasForeignKey(p => p.UserId)
       .OnDelete(DeleteBehavior.Cascade);





        // PCBuild configuration
        modelBuilder.Entity<PCBuild>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsPublic);
        });
    }
}
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Services.Scrapers;

public class ComponentUpsertService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ComponentUpsertService> _logger;

    public ComponentUpsertService(
        ApplicationDbContext context,
        ILogger<ComponentUpsertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int added, int updated)> UpsertComponentsAsync(
        List<Component> scrapedComponents)
    {
        int added = 0;
        int updated = 0;

        foreach (var scraped in scrapedComponents)
        {
            var existing = await _context.Components
                .Include(c => c.CPUSpec)
                .Include(c => c.GPUSpec)
                .Include(c => c.PSUSpec)
                .Include(c => c.RAMSpec)
                .Include(c => c.StorageSpec)
                .Include(c => c.MotherboardSpec)
                .Include(c => c.CPUCoolerSpec)
                .Include(c => c.CaseSpec)
                .FirstOrDefaultAsync(c =>
                    c.Name == scraped.Name &&
                    c.Type == scraped.Type);

            if (existing == null)
            {
                PrepareNewComponent(scraped);
                _context.Components.Add(scraped);
                added++;
                continue;
            }

            UpdateBaseFields(existing, scraped);
            UpdateSpecifications(existing, scraped);
            HandlePriceHistory(existing, scraped);

            updated++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Upsert complete: {Added} added, {Updated} updated",
            added, updated);

        return (added, updated);
    }

    private static void UpdateBaseFields(Component existing, Component scraped)
    {
        existing.LowestPrice = scraped.LowestPrice;
        existing.AveragePrice = scraped.AveragePrice;
        existing.ImageUrl = scraped.ImageUrl ?? existing.ImageUrl;
        existing.Manufacturer = scraped.Manufacturer;
        existing.OfferCount = scraped.OfferCount;
        existing.Rating = scraped.Rating;
        existing.ReviewCount = scraped.ReviewCount;
        existing.QualityScore = scraped.QualityScore;
        existing.LastUpdated = DateTime.UtcNow;
    }
    private static void PrepareNewComponent(Component component)
    {
        component.CreatedAt = DateTime.UtcNow;
        component.LastUpdated = DateTime.UtcNow;
    }
    private void HandlePriceHistory(Component existing, Component scraped)
    {
        if (!scraped.LowestPrice.HasValue)
            return;

        if (existing.LowestPrice == scraped.LowestPrice)
            return;

        _context.PriceHistories.Add(new PriceHistory
        {
            ComponentId = existing.Id,
            Price = scraped.LowestPrice.Value,
            Retailer = RetailerSource.Geizhals,
            RecordedAt = DateTime.UtcNow,
            InStock = true
        });
    }


    private static void UpdateSpecifications(Component existing, Component scraped)
    {
        if (scraped.CPUSpec != null)
            existing.CPUSpec = scraped.CPUSpec;

        if (scraped.GPUSpec != null)
            existing.GPUSpec = scraped.GPUSpec;

        if (scraped.PSUSpec != null)
            existing.PSUSpec = scraped.PSUSpec;

        if (scraped.RAMSpec != null)
            existing.RAMSpec = scraped.RAMSpec;

        if (scraped.StorageSpec != null)
            existing.StorageSpec = scraped.StorageSpec;

        if (scraped.MotherboardSpec != null)
            existing.MotherboardSpec = scraped.MotherboardSpec;

        if (scraped.CPUCoolerSpec != null)
            existing.CPUCoolerSpec = scraped.CPUCoolerSpec;

        if (scraped.CaseSpec != null)
            existing.CaseSpec = scraped.CaseSpec;
    }



}




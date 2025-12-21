using System.Text.Json;
using Domain.Enums;

namespace Domain.Entities;

public class Component : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Model { get; set; }
    public ComponentType Type { get; set; }
    public Manufacturer Manufacturer { get; set; }

    // Store specifications as JSON for flexibility
    public string? SpecificationsJson { get; set; }

    // Helper methods to work with specs
    public T? GetSpecifications<T>() where T : class
    {
        if (string.IsNullOrEmpty(SpecificationsJson))
            return null;
        return JsonSerializer.Deserialize<T>(SpecificationsJson);
    }

    public void SetSpecifications<T>(T specs) where T : class
    {
        SpecificationsJson = JsonSerializer.Serialize(specs);
    }

    // For search and matching
    public string? EAN { get; set; }
    public string? PartNumber { get; set; }

    // Image
    public string? ImageUrl { get; set; }

    // Average price (calculated field, updated by background job)
    public decimal? AveragePrice { get; set; }
    public decimal? LowestPrice { get; set; }

    // Relationships
    public ICollection<Price> Prices { get; set; } = new List<Price>();
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
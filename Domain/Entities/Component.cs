using System.Text.Json;
using Domain.Enums;

namespace Domain.Entities;

public class Component : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ComponentType Type { get; set; }
    public Manufacturer Manufacturer { get; set; }

    public string? ImageUrl { get; set; }

    public decimal? AveragePrice { get; set; }
    public decimal? LowestPrice { get; set; }
    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }

    public double QualityScore { get; set; }
    public int OfferCount { get; set; }
    public bool IsQualified { get; set; }

    public DateTime? LastUpdated { get; set; }

    // Navigation properties to specifications (one-to-one)
    public CPUSpecification? CPUSpec { get; set; }
    public GPUSpecification? GPUSpec { get; set; }
    public PSUSpecification? PSUSpec { get; set; }
    public RAMSpecification? RAMSpec { get; set; }

    public MotherboardSpec?  MotherboardSpec { get; set; }
    public StorageSpecification? StorageSpec { get; set; }
    public CPUCoolerSpecification? CPUCoolerSpec { get; set; }
    public CaseSpecification? CaseSpec { get; set; }
    // Relationships
    public ICollection<Price> Prices { get; set; } = new List<Price>();
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
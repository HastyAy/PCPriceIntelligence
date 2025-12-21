using Domain.Enums;

namespace Domain.Entities;

public class Price : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";

    public RetailerSource Retailer { get; set; }
    public string RetailerUrl { get; set; } = string.Empty;

    public bool InStock { get; set; } = true;

    // Shipping info
    public decimal? ShippingCost { get; set; }

    // For tracking when price was scraped
    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;
}
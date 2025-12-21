using Domain.Enums;

namespace Domain.Entities;

public class PriceHistory : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public decimal Price { get; set; }
    public RetailerSource Retailer { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public bool InStock { get; set; } = true;
}
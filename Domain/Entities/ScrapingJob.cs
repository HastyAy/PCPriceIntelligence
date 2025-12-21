using Domain.Enums;

namespace Domain.Entities;

public class ScrapingJob : BaseEntity
{
    public RetailerSource Retailer { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int ComponentsScraped { get; set; }
    public int PricesUpdated { get; set; }
    public int Errors { get; set; }

    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
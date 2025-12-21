namespace Domain.Entities;

public class SearchQuery : BaseEntity
{
    public string? UserId { get; set; }

    public string Query { get; set; } = string.Empty;  // "Gaming PC für 1500€"

    // Claude's parsed intent stored as JSON
    public string? ParsedIntentJson { get; set; }

    public int ResultCount { get; set; }

    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
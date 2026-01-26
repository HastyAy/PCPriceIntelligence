namespace Domain.Entities;

public class PCBuild : BaseEntity
{
    public string? UserId { get; set; }

    public string Name { get; set; } = string.Empty;  // "My Gaming Build"

    public decimal TotalPrice { get; set; }

    // Store component IDs as JSON array
    // Example: {"GPU": 123, "CPU": 456, "RAM": 789}
    public string ComponentsJson { get; set; } = string.Empty;

    public bool IsPublic { get; set; } = false;

    public string? Notes { get; set; } 
}
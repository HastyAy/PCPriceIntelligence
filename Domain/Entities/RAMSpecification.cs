namespace Domain.Entities;

public class RAMSpecification : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public int Capacity { get; set; }
    public string? Type { get; set; }
    public int Speed { get; set; }
    public string? Timings { get; set; }
    public int ModuleCount { get; set; }
}
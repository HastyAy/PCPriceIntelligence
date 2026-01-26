namespace Domain.Entities;

public class StorageSpecification : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;
    public string? Type { get; set; } 
    public int Capacity { get; set; }
    public string? Interface { get; set; }

}
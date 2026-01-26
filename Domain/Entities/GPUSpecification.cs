namespace Domain.Entities;

public class GPUSpecification : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public string MemoryType { get; set; } = string.Empty;  
    public int MemorySize { get; set; } 

    public string? Chipset { get; set; }  

    public int? TDP { get; set; }  

    public int? LengthMM { get; set; } 
    public int? WidthMM { get; set; }
    public int? HeightMM { get; set; }

    public int? Aux6PinCount { get; set; }  
    public int? Aux8PinCount { get; set; }  
}
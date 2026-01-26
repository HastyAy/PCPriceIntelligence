namespace Domain.Entities;

public class CPUCoolerSpecification : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;
    public string? SocketCompatibility { get; set; }

    public int? MaxTDP { get; set; }

    public int? HeightMM { get; set; }

    public bool IsLiquidCooled { get; set; }

    public int? FanCount { get; set; }
}
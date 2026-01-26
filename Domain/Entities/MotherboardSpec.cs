namespace Domain.Entities;

public class MotherboardSpec : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public string? Socket { get; set; }


    public string? Chipset { get; set; }

    public string? FormFactor { get; set; }

    public string? MemoryType { get; set; }

    public int? MemorySlots { get; set; }

    public int? MaxMemorySpeedMHz { get; set; }

    public string? PowerConnectors { get; set; }
    public int? MaxMemoryCapacityGB { get; set; }

    public int? M2SlotCount { get; set; }

    public string? PCIeSlots { get; set; }

    public string? MaxPCIeGeneration { get; set; }


}
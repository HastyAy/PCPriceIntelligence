namespace Domain.Entities;

public class CaseSpecification : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public string? FormFactor { get; set; }

    public int? MaxGPULengthMM { get; set; }

    public int? MaxCoolerHeightMM { get; set; }

    public int? BayCount35 { get; set; }

    public int? BayCount25 { get; set; }

    public string? ExpansionSlots { get; set; }


    public bool HasUSBC { get; set; }


    public bool HasUSB3 { get; set; }

    public decimal? VolumeLiters { get; set; }

    public string? DimensionsMM { get; set; }

    /// <summary>
    /// Has tempered glass panel
    /// </summary>
    public bool HasTemperedGlass { get; set; }
}
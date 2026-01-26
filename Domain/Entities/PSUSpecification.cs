namespace Domain.Entities;

public class PSUSpecification : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public int Wattage { get; set; }

    public bool Modular { get; set; }

    public string? EfficiencyRating { get; set; }

    public int? Aux6PinCount { get; set; }

    public int? SATAPowerCount { get; set; }

    public string? DimensionsMM { get; set; }

}
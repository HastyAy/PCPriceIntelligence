namespace Domain.Entities;

public class CPUSpecification : BaseEntity
{
    public int ComponentId { get; set; }
    public Component Component { get; set; } = null!;

    public int Cores { get; set; }
    public int Threads { get; set; }
    public decimal BaseClock { get; set; }
    public decimal? BoostClock { get; set; }
    public int TDP { get; set; }
    public bool IntegratedGraphics { get; set; }
}
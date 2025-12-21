namespace Domain.Entities
{
    public class GPUSpecification : BaseEntity
    {
        public int VRAM { get; set; }
        public string? Chipset { get; set; }
        public int? CoreClock { get; set; }
        public int? BoostClock { get; set; }
        public int? TDP { get; set; }
        public string? Interface { get; set; }
    }
}
namespace Domain.Entities
{
    public class PSUSpecification : BaseEntity
    {
        public int Wattage { get; set; }
        public string? Efficiency { get; set; }
        public bool Modular { get; set; }
    }
}
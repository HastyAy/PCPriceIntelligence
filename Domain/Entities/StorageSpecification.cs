namespace Domain.Entities
{
    public class StorageSpecification : BaseEntity
    {
        public int Capacity { get; set; }
        public string? Interface { get; set; }
        public string? FormFactor { get; set; }
        public int? ReadSpeed { get; set; }
        public int? WriteSpeed { get; set; }
    }
}
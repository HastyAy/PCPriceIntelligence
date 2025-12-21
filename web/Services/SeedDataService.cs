using Domain.Entities;
using Domain.Enums;
using web.Data;

namespace web.Services;

public class SeedDataService
{
    private readonly ApplicationDbContext _context;

    public SeedDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedDatabaseAsync()
    {
        // Check if already seeded
        if (_context.Components.Any())
        {
            return; // Already has data
        }

        var components = GetSampleComponents();
        _context.Components.AddRange(components);
        await _context.SaveChangesAsync();

        // Add prices for each component
        foreach (var component in components)
        {
            var prices = GeneratePricesForComponent(component);
            _context.Prices.AddRange(prices);
        }

        await _context.SaveChangesAsync();
    }

    private List<Component> GetSampleComponents()
    {
        return new List<Component>
        {
            // GPUs
            new Component
            {
                Name = "NVIDIA GeForce RTX 4090",
                Model = "RTX 4090",
                Type = ComponentType.GPU,
                Manufacturer = Manufacturer.NVIDIA,
                PartNumber = "RTX4090-24GB",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new GPUSpecification
                {
                    VRAM = 24,
                    Chipset = "AD102",
                    TDP = 450,
                    Interface = "PCIe 4.0 x16"
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=RTX+4090",
                AveragePrice = 1899.99m,
                LowestPrice = 1799.99m
            },
            new Component
            {
                Name = "NVIDIA GeForce RTX 4070",
                Model = "RTX 4070",
                Type = ComponentType.GPU,
                Manufacturer = Manufacturer.NVIDIA,
                PartNumber = "RTX4070-12GB",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new GPUSpecification
                {
                    VRAM = 12,
                    Chipset = "AD104",
                    TDP = 200,
                    Interface = "PCIe 4.0 x16"
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=RTX+4070",
                AveragePrice = 649.99m,
                LowestPrice = 599.99m
            },
            new Component
            {
                Name = "AMD Radeon RX 7900 XTX",
                Model = "RX 7900 XTX",
                Type = ComponentType.GPU,
                Manufacturer = Manufacturer.AMD_GPU,
                PartNumber = "RX7900XTX-24GB",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new GPUSpecification
                {
                    VRAM = 24,
                    Chipset = "Navi 31",
                    TDP = 355,
                    Interface = "PCIe 4.0 x16"
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=RX+7900+XTX",
                AveragePrice = 999.99m,
                LowestPrice = 949.99m
            },
            
            // CPUs
            new Component
            {
                Name = "AMD Ryzen 9 7950X",
                Model = "7950X",
                Type = ComponentType.CPU,
                Manufacturer = Manufacturer.AMD,
                PartNumber = "100-100000514WOF",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new CPUSpecification
                {
                    Cores = 16,
                    Threads = 32,
                    BaseClock = 4.5m,
                    BoostClock = 5.7m,
                    TDP = 170,
                    Socket = "AM5"
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=Ryzen+9+7950X",
                AveragePrice = 599.99m,
                LowestPrice = 549.99m
            },
            new Component
            {
                Name = "Intel Core i9-14900K",
                Model = "i9-14900K",
                Type = ComponentType.CPU,
                Manufacturer = Manufacturer.Intel,
                PartNumber = "BX8071514900K",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new CPUSpecification
                {
                    Cores = 24,
                    Threads = 32,
                    BaseClock = 3.2m,
                    BoostClock = 6.0m,
                    TDP = 125,
                    Socket = "LGA1700"
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=i9-14900K",
                AveragePrice = 589.99m,
                LowestPrice = 559.99m
            },
            
            // RAM
            new Component
            {
                Name = "Corsair Vengeance DDR5 32GB (2x16GB) 6000MHz",
                Model = "CMK32GX5M2D6000C36",
                Type = ComponentType.RAM,
                Manufacturer = Manufacturer.Corsair,
                PartNumber = "CMK32GX5M2D6000C36",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new RAMSpecification
                {
                    Capacity = 32,
                    Type = "DDR5",
                    Speed = 6000,
                    Timings = "CL36-36-36-76",
                    ModuleCount = 2
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=DDR5+32GB",
                AveragePrice = 139.99m,
                LowestPrice = 129.99m
            },
            
            // SSDs
            new Component
            {
                Name = "Samsung 990 PRO 2TB NVMe SSD",
                Model = "990 PRO",
                Type = ComponentType.SSD,
                Manufacturer = Manufacturer.Samsung,
                PartNumber = "MZ-V9P2T0BW",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new StorageSpecification
                {
                    Capacity = 2000,
                    Interface = "NVMe PCIe 4.0",
                    FormFactor = "M.2 2280",
                    ReadSpeed = 7450,
                    WriteSpeed = 6900
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=990+PRO",
                AveragePrice = 179.99m,
                LowestPrice = 169.99m
            },
            
            // PSUs
            new Component
            {
                Name = "Corsair RM850e 850W 80+ Gold",
                Model = "RM850e",
                Type = ComponentType.PSU,
                Manufacturer = Manufacturer.Corsair_PSU,
                PartNumber = "CP-9020248-EU",
                SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(new PSUSpecification
                {
                    Wattage = 850,
                    Efficiency = "80+ Gold",
                    Modular = true
                }),
                ImageUrl = "https://via.placeholder.com/300x200?text=RM850e",
                AveragePrice = 129.99m,
                LowestPrice = 119.99m
            }
        };
    }

    private List<Price> GeneratePricesForComponent(Component component)
    {
        var random = new Random();
        var basePrice = component.LowestPrice ?? 100m;

        return new List<Price>
        {
            new Price
            {
                Component = component,
                Amount = basePrice,
                Currency = "EUR",
                Retailer = RetailerSource.Mindfactory,
                RetailerUrl = $"https://www.mindfactory.de/product/{component.PartNumber}",
                InStock = true,
                ShippingCost = 0m,
                ScrapedAt = DateTime.UtcNow
            },
            new Price
            {
                Component = component,
                Amount = basePrice + (decimal)(random.NextDouble() * 50),
                Currency = "EUR",
                Retailer = RetailerSource.Alternate,
                RetailerUrl = $"https://www.alternate.de/product/{component.PartNumber}",
                InStock = true,
                ShippingCost = 5.99m,
                ScrapedAt = DateTime.UtcNow
            },
            new Price
            {
                Component = component,
                Amount = basePrice + (decimal)(random.NextDouble() * 30),
                Currency = "EUR",
                Retailer = RetailerSource.Amazon,
                RetailerUrl = $"https://www.amazon.de/dp/{component.PartNumber}",
                InStock = random.Next(0, 10) > 1,
                ShippingCost = 0m,
                ScrapedAt = DateTime.UtcNow
            }
        };
    }
}
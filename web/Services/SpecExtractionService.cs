using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace web.Services;

public class SpecExtractionService
{
    private readonly ILogger<SpecExtractionService> _logger;

    public SpecExtractionService(ILogger<SpecExtractionService> logger)
    {
        _logger = logger;
    }


    public CPUSpecification? ExtractCPUSpec(string name)
    {
        try
        {
            var spec = new CPUSpecification();

            // Extract cores/threads (8C/16T, 6-Core, 12 Threads, etc.)
            var coreThreadMatch = Regex.Match(name, @"(\d+)C[/\s]*(\d+)T", RegexOptions.IgnoreCase);
            if (coreThreadMatch.Success)
            {
                spec.Cores = int.Parse(coreThreadMatch.Groups[1].Value);
                spec.Threads = int.Parse(coreThreadMatch.Groups[2].Value);
            }
            else
            {
                var coreMatch = Regex.Match(name, @"(\d+)[-\s]Core", RegexOptions.IgnoreCase);
                if (coreMatch.Success)
                {
                    spec.Cores = int.Parse(coreMatch.Groups[1].Value);
                    spec.Threads = spec.Cores * 2;
                }
            }

            // Extract clock speeds (4.20-5.00GHz, 3.6GHz, etc.)
            var clockMatch = Regex.Match(name, @"(\d+\.?\d*)-?(\d+\.?\d*)\s*GHz", RegexOptions.IgnoreCase);
            if (clockMatch.Success)
            {
                if (double.TryParse(clockMatch.Groups[1].Value, out var baseClock))
                {
                    spec.BaseClock = (decimal)baseClock;
                }
                if (clockMatch.Groups[2].Success && double.TryParse(clockMatch.Groups[2].Value, out var boostClock))
                {
                    spec.BoostClock = (decimal)boostClock;
                }
                else
                {
                    spec.BoostClock = spec.BaseClock;
                }
            }


            // Detect integrated graphics
            spec.IntegratedGraphics = name.Contains("integrated", StringComparison.OrdinalIgnoreCase) ||
                                     Regex.IsMatch(name, @"\b(Ryzen.*G|Core.*[FGT])\b", RegexOptions.IgnoreCase);

            // Extract TDP (65W, 125W, etc.)
            var tdpMatch = Regex.Match(name, @"(\d+)\s*W(?:att)?", RegexOptions.IgnoreCase);
            if (tdpMatch.Success)
            {
                spec.TDP = int.Parse(tdpMatch.Groups[1].Value);
            }
            else
            {
                if (spec.Cores > 0)
                {
                    spec.TDP = spec.Cores >= 12 ? 125 : (spec.Cores >= 8 ? 105 : 65);
                }
            }

            return spec.Cores > 0 ? spec : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract CPU specs from: {Name}", name);
            return null;
        }
    }

    public RAMSpecification? ExtractRAMSpec(string name)
    {
        try
        {
            var spec = new RAMSpecification();

            // Extract capacity (32GB, 16GB, Kit 64GB, etc.)
            var capacityMatch = Regex.Match(name, @"(?:Kit\s+)?(\d+)\s*GB", RegexOptions.IgnoreCase);
            if (capacityMatch.Success)
            {
                spec.Capacity = int.Parse(capacityMatch.Groups[1].Value);
            }

            // Extract type (DDR5, DDR4)
            var typeMatch = Regex.Match(name, @"DDR\d+", RegexOptions.IgnoreCase);
            if (typeMatch.Success)
            {
                spec.Type = typeMatch.Value.ToUpper();
            }

            // Extract speed (DDR4-3200, 3600MHz, etc.)
            var speedMatch = Regex.Match(name, @"DDR\d+-?(\d+)", RegexOptions.IgnoreCase);
            if (speedMatch.Success)
            {
                spec.Speed = int.Parse(speedMatch.Groups[1].Value);
            }

            // Extract module count (Kit 2x16GB means 2 modules)
            var moduleMatch = Regex.Match(name, @"(\d+)x\d+GB", RegexOptions.IgnoreCase);
            if (moduleMatch.Success)
            {
                spec.ModuleCount = int.Parse(moduleMatch.Groups[1].Value);
            }
            else
            {
                spec.ModuleCount = name.Contains("Kit", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
            }

            // Extract timings (CL16, CL18-22-22-42, etc.)
            var timingsMatch = Regex.Match(name, @"CL(\d+(?:-\d+)*)", RegexOptions.IgnoreCase);
            if (timingsMatch.Success)
            {
                spec.Timings = "CL" + timingsMatch.Groups[1].Value;
            }

            return spec.Capacity > 0 ? spec : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract RAM specs from: {Name}", name);
            return null;
        }
    }

    public StorageSpecification? ExtractStorageSpec(string name)
    {
        try
        {
            var spec = new StorageSpecification();

            // Extract capacity (1TB, 500GB, 2000GB, etc.)
            var capacityMatch = Regex.Match(name, @"(\d+(?:\.\d+)?)\s*(TB|GB)", RegexOptions.IgnoreCase);
            if (capacityMatch.Success)
            {
                var value = double.Parse(capacityMatch.Groups[1].Value);
                var unit = capacityMatch.Groups[2].Value.ToUpper();
                spec.Capacity = unit == "TB" ? (int)(value * 1000) : (int)value;
            }

            // Determine type
            if (name.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
            {
                spec.Type = "NVMe SSD";
            }
            else if (name.Contains("M.2", StringComparison.OrdinalIgnoreCase))
            {
                spec.Type = "M.2 SSD";
            }
            else if (name.Contains("SATA", StringComparison.OrdinalIgnoreCase) || name.Contains("2.5\""))
            {
                spec.Type = "SATA SSD";
            }
            else
            {
                spec.Type = "SSD";
            }

            // Extract interface
            if (name.Contains("PCIe 5", StringComparison.OrdinalIgnoreCase))
            {
                spec.Interface = "PCIe 5.0";
            }
            else if (name.Contains("PCIe 4", StringComparison.OrdinalIgnoreCase))
            {
                spec.Interface = "PCIe 4.0";
            }
            else if (name.Contains("PCIe 3", StringComparison.OrdinalIgnoreCase))
            {
                spec.Interface = "PCIe 3.0";
            }
            else if (name.Contains("SATA"))
            {
                spec.Interface = "SATA";
            }

            return spec.Capacity > 0 ? spec : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract Storage specs from: {Name}", name);
            return null;
        }
    }



    

   


   

   
}
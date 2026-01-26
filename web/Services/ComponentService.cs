using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Services;

public class ComponentService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    // Static storage for prebuilt configurations (thread-safe)
    private static readonly Dictionary<string, Dictionary<ComponentType, int>> PrebuiltConfigurations = new();
    private static readonly object _lock = new();

    public ComponentService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Component?> GetComponentByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Components
            .Include(c => c.CPUSpec)
            .Include(c => c.GPUSpec)
            .Include(c => c.PSUSpec)
            .Include(c => c.RAMSpec)
            .Include(c => c.MotherboardSpec)
            .Include(c => c.StorageSpec)
            .Include(c => c.CPUCoolerSpec)
            .Include(c => c.CaseSpec)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Component>> GetComponentsByCategoryAsync(ComponentType type, int limit = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Components
            .Include(c => c.CPUSpec)
            .Include(c => c.GPUSpec)
            .Include(c => c.PSUSpec)
            .Include(c => c.RAMSpec)
            .Include(c => c.MotherboardSpec)
            .Include(c => c.StorageSpec)
            .Include(c => c.CPUCoolerSpec)
            .Include(c => c.CaseSpec)
            .Where(c => c.Type == type)
            .OrderByDescending(c => c.QualityScore)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<Component>> GetAllComponentsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Components
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    // ========== Prebuilt Configuration Methods ==========

    public void StorePrebuiltConfiguration(string sessionId, Dictionary<ComponentType, Component> components)
    {
        lock (_lock)
        {
            PrebuiltConfigurations[sessionId] = components.ToDictionary(k => k.Key, v => v.Value.Id);

            // Clean up old sessions (keep last 100)
            if (PrebuiltConfigurations.Count > 100)
            {
                var oldest = PrebuiltConfigurations.Keys.First();
                PrebuiltConfigurations.Remove(oldest);
            }
        }
    }

    public async Task<Dictionary<ComponentType, Component>> GetPrebuiltConfigurationAsync(string sessionId)
    {
        Dictionary<ComponentType, int>? componentIds;

        lock (_lock)
        {
            if (!PrebuiltConfigurations.TryGetValue(sessionId, out componentIds))
                return new();

            PrebuiltConfigurations.Remove(sessionId);
        }

        var result = new Dictionary<ComponentType, Component>();

        // Load all components in a single query to avoid multiple DbContext issues
        await using var context = await _contextFactory.CreateDbContextAsync();

        var ids = componentIds.Values.ToList();
        var components = await context.Components
            .Include(c => c.CPUSpec)
            .Include(c => c.GPUSpec)
            .Include(c => c.PSUSpec)
            .Include(c => c.RAMSpec)
            .Include(c => c.MotherboardSpec)
            .Include(c => c.StorageSpec)
            .Include(c => c.CPUCoolerSpec)
            .Include(c => c.CaseSpec)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

        foreach (var kvp in componentIds)
        {
            var component = components.FirstOrDefault(c => c.Id == kvp.Value);
            if (component != null)
                result[kvp.Key] = component;
        }

        return result;
    }

    // ========== Smart Build Algorithm ==========

    public async Task<Dictionary<ComponentType, Component>> BuildOptimalConfigurationAsync(
        decimal budget,
        string useCase,
        string priority)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var result = new Dictionary<ComponentType, Component>();
        var allocation = GetBudgetAllocation(useCase);

        var selectionOrder = new[]
        {
            ComponentType.CPU,
            ComponentType.Motherboard,
            ComponentType.GPU,
            ComponentType.RAM,
            ComponentType.PSU,
            ComponentType.SSD,
            ComponentType.Case,
            ComponentType.Cooling
        };

        // Load all potential components upfront to avoid multiple queries
        var allComponents = await context.Components
            .Include(c => c.CPUSpec)
            .Include(c => c.GPUSpec)
            .Include(c => c.PSUSpec)
            .Include(c => c.RAMSpec)
            .Include(c => c.MotherboardSpec)
            .Include(c => c.StorageSpec)
            .Include(c => c.CPUCoolerSpec)
            .Include(c => c.CaseSpec)
            .Where(c => c.LowestPrice > 0)
            .ToListAsync();

        decimal remainingBudget = budget;

        foreach (var type in selectionOrder)
        {
            if (!allocation.ContainsKey(type)) continue;

            var targetPrice = budget * allocation[type];
            var maxPrice = Math.Min(targetPrice * 1.3m, remainingBudget * 0.7m);
            var minPrice = targetPrice * 0.4m;

            var candidates = allComponents
                .Where(c => c.Type == type)
                .Where(c => c.LowestPrice >= minPrice && c.LowestPrice <= maxPrice)
                .ToList();

            // Fallback if no candidates in range
            if (!candidates.Any())
            {
                candidates = allComponents
                    .Where(c => c.Type == type)
                    .Where(c => c.LowestPrice <= maxPrice * 1.5m)
                    .OrderBy(c => c.LowestPrice)
                    .Take(10)
                    .ToList();
            }

            if (!candidates.Any()) continue;

            // Filter for compatibility
            candidates = FilterCompatibleComponents(candidates, result);

            if (!candidates.Any()) continue;

            // Score and select
            var best = candidates
                .OrderByDescending(c => CalculateScore(c, priority))
                .First();

            result[type] = best;
            remainingBudget -= best.LowestPrice ?? 0;
        }

        return result;
    }

    private Dictionary<ComponentType, decimal> GetBudgetAllocation(string useCase)
    {
        return useCase.ToLower() switch
        {
            "gaming" => new Dictionary<ComponentType, decimal>
            {
                { ComponentType.CPU, 0.18m },
                { ComponentType.GPU, 0.35m },
                { ComponentType.Motherboard, 0.12m },
                { ComponentType.RAM, 0.08m },
                { ComponentType.PSU, 0.08m },
                { ComponentType.SSD, 0.08m },
                { ComponentType.Case, 0.06m },
                { ComponentType.Cooling, 0.05m }
            },
            "workstation" => new Dictionary<ComponentType, decimal>
            {
                { ComponentType.CPU, 0.30m },
                { ComponentType.GPU, 0.20m },
                { ComponentType.Motherboard, 0.15m },
                { ComponentType.RAM, 0.12m },
                { ComponentType.PSU, 0.08m },
                { ComponentType.SSD, 0.08m },
                { ComponentType.Case, 0.04m },
                { ComponentType.Cooling, 0.03m }
            },
            "office" => new Dictionary<ComponentType, decimal>
            {
                { ComponentType.CPU, 0.25m },
                { ComponentType.Motherboard, 0.20m },
                { ComponentType.RAM, 0.15m },
                { ComponentType.PSU, 0.12m },
                { ComponentType.SSD, 0.15m },
                { ComponentType.Case, 0.08m },
                { ComponentType.Cooling, 0.05m }
            },
            _ => new Dictionary<ComponentType, decimal>
            {
                { ComponentType.CPU, 0.20m },
                { ComponentType.GPU, 0.30m },
                { ComponentType.Motherboard, 0.12m },
                { ComponentType.RAM, 0.10m },
                { ComponentType.PSU, 0.08m },
                { ComponentType.SSD, 0.10m },
                { ComponentType.Case, 0.05m },
                { ComponentType.Cooling, 0.05m }
            }
        };
    }

    private List<Component> FilterCompatibleComponents(
        List<Component> candidates,
        Dictionary<ComponentType, Component> currentBuild)
    {
        var type = candidates.FirstOrDefault()?.Type;

        // RAM must match motherboard memory type
        if (type == ComponentType.RAM &&
            currentBuild.TryGetValue(ComponentType.Motherboard, out var mobo) &&
            mobo.MotherboardSpec != null)
        {
            var memType = mobo.MotherboardSpec.MemoryType;
            if (!string.IsNullOrEmpty(memType))
            {
                var compatible = candidates
                    .Where(c => c.RAMSpec?.Type?.Contains(memType, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                if (compatible.Any()) return compatible;
            }
        }

        return candidates;
    }

    private double CalculateScore(Component c, string priority)
    {
        var baseScore = c.QualityScore;
        var ratingBonus = (double)(c.Rating ?? 0) * Math.Log10(c.ReviewCount + 1);
        var priceScore = c.LowestPrice.HasValue && c.LowestPrice > 0
            ? 1000.0 / (double)c.LowestPrice.Value
            : 0;

        return priority.ToLower() switch
        {
            "performance" => baseScore * 1.5 + ratingBonus,
            "value" => baseScore + priceScore * 2 + ratingBonus,
            _ => baseScore + ratingBonus + priceScore
        };
    }
}
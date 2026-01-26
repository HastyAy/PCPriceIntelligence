using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Services;

public class BuildService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public BuildService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task SaveBuildAsync(PCBuild build)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.PCBuilds.Add(build);
        await context.SaveChangesAsync();
    }

    public async Task<PCBuild?> GetBuildAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PCBuilds.FindAsync(id);
    }

    public async Task<PCBuild?> GetBuildByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PCBuilds.FindAsync(id);
    }

    public async Task<List<PCBuild>> GetUserBuildsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PCBuilds
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<ComponentType, Component>> GetBuildComponentsAsync(PCBuild build)
    {
        var result = new Dictionary<ComponentType, Component>();

        if (string.IsNullOrEmpty(build.ComponentsJson))
            return result;

        try
        {
            var componentIds = JsonSerializer.Deserialize<Dictionary<string, int>>(build.ComponentsJson);
            if (componentIds == null)
                return result;

            await using var context = await _contextFactory.CreateDbContextAsync();

            // Load all components in a single query
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
                if (Enum.TryParse<ComponentType>(kvp.Key, true, out var componentType))
                {
                    var component = components.FirstOrDefault(c => c.Id == kvp.Value);
                    if (component != null)
                    {
                        result[componentType] = component;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing ComponentsJson: {ex.Message}");
        }

        return result;
    }

    public async Task UpdateBuildAsync(PCBuild build)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.PCBuilds.Update(build);
        await context.SaveChangesAsync();
    }

    public async Task DeleteBuildAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var build = await context.PCBuilds.FindAsync(id);
        if (build != null)
        {
            context.PCBuilds.Remove(build);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> UserOwnsBuildAsync(int buildId, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var build = await context.PCBuilds.FindAsync(buildId);
        return build?.UserId == userId;
    }
}
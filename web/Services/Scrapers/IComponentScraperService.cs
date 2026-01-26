using Domain.Entities;

namespace web.Services.Scrapers;

public interface IComponentScraperService
{
    Task<List<Component>> ScrapeComponentsAsync(ComponentCategory category, int maxResults = 20);
    Task<List<Component>> ScrapeComponentsAsync(string url, ComponentCategory category);
    string GetSourceName();
}

public enum ComponentCategory
{
    GPU,
    CPU,
    Motherboard,
    RAM,
    SSD,
    HDD,
    PSU,
    Case,
    Cooling,
}
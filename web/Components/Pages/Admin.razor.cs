using web.Services.Scrapers;

namespace web.Components.Pages
{
    public partial class Admin
    {
        private List<ComponentCategory> categories = Enum.GetValues<ComponentCategory>().ToList();
        private Dictionary<ComponentCategory, (int scraped, int added, int updated)> categoryResults = new();
        private Dictionary<ComponentCategory, string> categoryStatus = new();
        private List<(DateTime timestamp, string level, string message)> logs = new();

        private bool isScraperRunning = false;
        private int totalScraped = 0;
        private int totalAdded = 0;
        private int totalUpdated = 0;

        private async Task ScrapeAllCategories()
        {
            if (isScraperRunning) return;

            isScraperRunning = true;
            totalScraped = 0;
            totalAdded = 0;
            totalUpdated = 0;
            categoryResults.Clear();

            AddLog("info", "?? Starting full scrape of all categories...");

            foreach (var category in categories)
            {
                await ScrapeCategory(category);
                await Task.Delay(200);
            }

            isScraperRunning = false;
            AddLog("success", $"? Scraping complete! Total: {totalScraped} scraped, {totalAdded} added, {totalUpdated} updated");
        }

        private async Task ScrapeCategory(ComponentCategory category)
        {
            if (GetCategoryStatus(category) == "running") return;

            categoryStatus[category] = "running";
            AddLog("info", $"?? Scraping {category}...");
            StateHasChanged();

            try
            {
                // Scrape components
                var components = await ScraperService.ScrapeComponentsAsync(category, maxResults: 500);
                AddLog("success", $"? Found {components.Count} {category} components");

                // Upsert to database
                var (added, updated) = await UpsertService.UpsertComponentsAsync(components);

                categoryResults[category] = (components.Count, added, updated);
                totalScraped += components.Count;
                totalAdded += added;
                totalUpdated += updated;

                AddLog("success", $"?? {category}: {added} new, {updated} updated");
            }
            catch (Exception ex)
            {
                AddLog("error", $"? Failed to scrape {category}: {ex.Message}");

            }
            finally
            {
                categoryStatus[category] = "idle";
                StateHasChanged();
            }
        }

        private string GetCategoryStatus(ComponentCategory category)
        {
            return categoryStatus.GetValueOrDefault(category, "idle");
        }

        private string GetCategoryIcon(ComponentCategory category)
        {
            return category switch
            {
                ComponentCategory.CPU => "?? Processors",
                ComponentCategory.GPU => "?? Graphics Cards",
                ComponentCategory.Motherboard => "?? Motherboards",
                ComponentCategory.RAM => "?? Memory",
                ComponentCategory.SSD => "?? Storage",
                ComponentCategory.HDD => "?? Hard Drives",
                ComponentCategory.PSU => "? Power Supplies",
                ComponentCategory.Case => "?? Cases",
                ComponentCategory.Cooling => "?? Coolers",
                _ => "??"
            };
        }

        private void AddLog(string level, string message)
        {
            logs.Add((DateTime.Now, level, message));

            // Keep only last 100 logs
            if (logs.Count > 100)
            {
                logs = logs.Skip(logs.Count - 100).ToList();
            }
        }

        private string GetLogClass(string level)
        {
            return level switch
            {
                "info" => "log-info",
                "success" => "log-success",
                "warning" => "log-warning",
                "error" => "log-error",
                _ => ""
            };
        }

        private void ClearLogs()
        {
            logs.Clear();
        }
    }
}
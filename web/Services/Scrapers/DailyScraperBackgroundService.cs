namespace web.Services.Scrapers;

public class DailyScraperBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyScraperBackgroundService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _runTime;
    private readonly int _maxResultsPerCategory;

    public DailyScraperBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DailyScraperBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;

        // Read configuration
        var runTimeStr = _configuration["ScraperSettings:DailyRunTime"] ?? "02:00:00";
        _runTime = TimeSpan.Parse(runTimeStr);

        _maxResultsPerCategory = _configuration.GetValue("ScraperSettings:MaxResultsPerCategory", 100);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 Daily Scraper Background Service started");
        _logger.LogInformation("⏰ Configured to run daily at {RunTime}", _runTime);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = GetNextRunTime(now);
                var delay = nextRun - now;

                _logger.LogInformation("⏰ Next scrape scheduled for: {NextRun} (in {Hours}h {Minutes}m)",
                    nextRun, delay.Hours, delay.Minutes);

                // Wait until next run time
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RunDailyScrapeAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🛑 Daily Scraper Background Service stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Daily Scraper Background Service");
                // Wait before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 Daily Scraper Background Service stopped");
    }

    private DateTime GetNextRunTime(DateTime currentTime)
    {
        var scheduledTime = currentTime.Date.Add(_runTime);

        // If today's run time has passed, schedule for tomorrow
        if (currentTime >= scheduledTime)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        return scheduledTime;
    }

    private async Task RunDailyScrapeAsync()
    {
        _logger.LogInformation("🚀 Starting daily scrape at {Time}", DateTime.Now);

        using var scope = _serviceProvider.CreateScope();
        var scraper = scope.ServiceProvider.GetRequiredService<IComponentScraperService>();
        var upsertService = scope.ServiceProvider.GetRequiredService<ComponentUpsertService>();

        var enabledCategoriesConfig = _configuration.GetSection("ScraperSettings:EnabledCategories").Get<string[]>();

        if (enabledCategoriesConfig == null || enabledCategoriesConfig.Length == 0)
        {
            _logger.LogWarning("⚠️ No enabled categories found in configuration. Using defaults.");
            enabledCategoriesConfig = new[] { "CPU", "GPU", "RAM", "Motherboard", "SSD", "PSU", "Case", "Cooling" };
        }

        // Parse string categories to enum
        var categoriesToScrape = enabledCategoriesConfig
            .Select(c => Enum.TryParse<ComponentCategory>(c, true, out var category) ? (ComponentCategory?)category : null)
            .Where(c => c.HasValue)
            .Select(c => c.Value)
            .ToList();

        if (!categoriesToScrape.Any())
        {
            _logger.LogError("❌ No valid categories to scrape");
            return;
        }

        _logger.LogInformation("📋 Categories to scrape: {Categories}",
            string.Join(", ", categoriesToScrape.Select(c => c.ToString())));

        int totalAdded = 0;
        int totalUpdated = 0;

        foreach (var category in categoriesToScrape)
        {
            try
            {
                _logger.LogInformation("📦 Scraping {Category}...", category);

                // Scrape components
                var components = await scraper.ScrapeComponentsAsync(category, maxResults: _maxResultsPerCategory);
                _logger.LogInformation("✅ Scraped {Count} {Category} components", components.Count, category);

                // Upsert to database
                var (added, updated) = await upsertService.UpsertComponentsAsync(components);
                _logger.LogInformation("💾 {Category}: {Added} new, {Updated} updated", category, added, updated);

                totalAdded += added;
                totalUpdated += updated;

                // wait between categories
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to scrape {Category}", category);
            }
        }

        _logger.LogInformation("🎉 Daily scrape complete! Total: {Added} new, {Updated} updated",
            totalAdded, totalUpdated);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Stopping Daily Scraper Background Service...");
        return base.StopAsync(cancellationToken);
    }
}
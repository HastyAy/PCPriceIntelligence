using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Playwright;

namespace web.Services.Scrapers;

public class GeizhalsPlaywrightScraperService : IComponentScraperService
{
    private readonly ILogger<GeizhalsPlaywrightScraperService> _logger;
    private readonly SpecExtractionService _specExtractionService;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private static readonly Dictionary<ComponentCategory, PriceRange> PriceRanges = new()
    {
        { ComponentCategory.GPU, new PriceRange(150, 2500) },
        { ComponentCategory.CPU, new PriceRange(80, 1500) },
        { ComponentCategory.Motherboard, new PriceRange(60, 800) },
        { ComponentCategory.RAM, new PriceRange(25, 500) },
        { ComponentCategory.SSD, new PriceRange(30, 500) },
        { ComponentCategory.HDD, new PriceRange(40, 300) },
        { ComponentCategory.PSU, new PriceRange(40, 400) },
        { ComponentCategory.Case, new PriceRange(30, 300) },
        { ComponentCategory.Cooling, new PriceRange(10, 200) }
    };

    private static readonly Dictionary<ComponentCategory, string[]> CategoryUrls = new()
    {
        { ComponentCategory.GPU, new[] { "https://geizhals.de/?cat=gra16_512&sort=p&hloc=at&hloc=de&v=e" } },
        { ComponentCategory.CPU, new[] {
            "https://geizhals.de/?cat=cpu1151&sort=p&hloc=at&hloc=de&v=e",
            "https://geizhals.de/?cat=cpuamdam4&sort=p&hloc=at&hloc=de&v=e"
        }},
        { ComponentCategory.Motherboard, new[] { "https://geizhals.de/?cat=mainboards&sort=p&hloc=at&hloc=de&v=e" } },
        { ComponentCategory.RAM, new[] { "https://geizhals.de/?cat=ramddr3" } },
        { ComponentCategory.SSD, new[] { "https://geizhals.de/?cat=hdssd&sort=p&hloc=at&hloc=de&v=e" } },
        { ComponentCategory.HDD, new[] { "https://geizhals.de/?cat=hde7s&sort=p&hloc=at&hloc=de&v=e" } },
        { ComponentCategory.PSU, new[] { "https://geizhals.de/?cat=gehps&sort=p&hloc=at&hloc=de&v=e" } },
        { ComponentCategory.Case, new[] { "https://geizhals.de/?cat=gehatx&sort=p&hloc=at&hloc=de&v=e" } },
        { ComponentCategory.Cooling, new[] { "https://geizhals.de/?cat=cpucooler&sort=p&hloc=at&hloc=de&v=e" } }
    };

    public GeizhalsPlaywrightScraperService(ILogger<GeizhalsPlaywrightScraperService> logger, SpecExtractionService specExtractionService)
    {
        _logger = logger;
        _specExtractionService = specExtractionService;
    }

    public string GetSourceName() => "Geizhals.de (Browser)";

    public async Task<List<Component>> ScrapeComponentsAsync(ComponentCategory category, int maxResults = 100)
    {
        if (!CategoryUrls.TryGetValue(category, out var urls))
        {
            _logger.LogWarning("No category URL configured for {Category}", category);
            return new List<Component>();
        }

        var allComponents = new List<Component>();

        foreach (var url in urls)
        {
            var components = await ScrapeUrlWithBrowserAsync(url, category, maxResults);
            allComponents.AddRange(components);

            if (allComponents.Count >= maxResults)
            {
                break;
            }
        }

        // return allComponents.Take(maxResults).ToList();
        return FilterByQuality(allComponents, category, maxResults);
    }

    public async Task<List<Component>> ScrapeComponentsAsync(string url, ComponentCategory category)
    {
        return await ScrapeUrlWithBrowserAsync(url, category, 1000);
    }

    private async Task InitializeBrowserAsync()
    {
        if (_browser != null) return;

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        _logger.LogInformation(" Browser initialized");
    }

    private async Task<List<Component>> ScrapeUrlWithBrowserAsync(string url, ComponentCategory category, int maxResults)
    {
        var components = new List<Component>();

        try
        {
            await InitializeBrowserAsync();

            var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                Locale = "de-DE",
                TimezoneId = "Europe/Berlin"
            });

            var page = await context.NewPageAsync();

            var baseUrl = url.Contains("view=") ? url : url + "&view=gallery";
            int pageNumber = 1;
            int consecutiveEmptyPages = 0;
            int maxPages = 60; 
            int qualifiedCount = 0;

            while (qualifiedCount < maxResults &&
                   pageNumber <= maxPages &&
                   consecutiveEmptyPages < 4)
            {
                var galleryUrl = pageNumber == 1 ? baseUrl : $"{baseUrl}&pg={pageNumber}";
                await page.GotoAsync(galleryUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });

                var elements = await page
                    .Locator("article.galleryview__item, div.galleryview__item")
                    .AllAsync();

                if (elements.Count == 0)
                {
                    consecutiveEmptyPages++;
                    pageNumber++;
                    continue;
                }

                consecutiveEmptyPages = 0;

                foreach (var element in elements)
                {
                    if (qualifiedCount >= maxResults)
                        break;

                    var component = await ParseProductElementAsync(element, category);
                    if (component == null)
                        continue;

                    // OUTSIDE PAGE
                    await ExtractListPageQualitySignalsAsync(element, component);
                    component.QualityScore = CalculateQualityScore(component);

                    // QUALITY GATE
                    if (!ShouldScrapeDetails(component))
                    {
                        component.IsQualified = false;
                        continue;
                    }

                    // DETAILS PAGE
                    switch (category)
                    {
                        case ComponentCategory.Motherboard:
                            component.MotherboardSpec =
                                await ExtractMotherboardSpecsFromDetailsPageAsync(element, page);
                            break;

                        case ComponentCategory.Case:
                            component.CaseSpec =
                                await ExtractCaseSpecsFromDetailsPageAsync(element, page);
                            break;

                        case ComponentCategory.GPU:
                            component.GPUSpec =
                                await ExtractGPUSpecsFromDetailsPageAsync(element, page);
                            break;

                        case ComponentCategory.Cooling:
                            component.CPUCoolerSpec =
                                await ExtractCoolerSpecsFromDetailsPageAsync(element, page);
                            break;

                        case ComponentCategory.PSU:
                            component.PSUSpec =
                                await ExtractPSUSpecsFromDetailsPageAsync(element, page);
                            break;

                        default:
                            ExtractAndAttachSpecs(component, component.Name);
                            break;
                    }
                    _logger.LogDebug(
                                "Rejected after details | {Name} | Category={Category}",
                                component.Name,
                                category);
                    if (!HasSpecification(component, category))
                        continue;

                    component.IsQualified = true;
                    qualifiedCount++;
                    components.Add(component);

                    _logger.LogInformation(
                        "✅ Qualified {Name} ({Qualified}/{Max})",
                        component.Name,
                        qualifiedCount,
                        maxResults);
                }

                pageNumber++;
            }


            await page.CloseAsync();
            await context.CloseAsync();

            _logger.LogInformation("✅ Scraping complete: {Count} components from {Url}", components.Count, url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to scrape {Url}", url);
        }

        return components;
    }
    private bool HasSpecification(Component component, ComponentCategory category)
    {
        return category switch
        {
            ComponentCategory.CPU =>
                component.CPUSpec != null,

            ComponentCategory.GPU =>
                component.GPUSpec != null,

            ComponentCategory.RAM =>
                component.RAMSpec != null,

            ComponentCategory.SSD or ComponentCategory.HDD =>
                component.StorageSpec != null,

            ComponentCategory.Motherboard =>
                component.MotherboardSpec != null,

            ComponentCategory.Case =>
                component.CaseSpec != null,

            ComponentCategory.Cooling =>
                component.CPUCoolerSpec != null,

            ComponentCategory.PSU =>
                component.PSUSpec != null,

            _ => false
        };
    }

    private async Task<CaseSpecification?> ExtractCaseSpecsFromDetailsPageAsync(
    ILocator element,
    IPage currentPage)
    {
        IPage? detailsPage = null;

        try
        {
            var href = await element.Locator("a").First.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href))
                return null;

            if (!href.StartsWith("http"))
                href = "https://geizhals.de/" + href.TrimStart('/');

            detailsPage = await currentPage.Context.NewPageAsync();
            await detailsPage.GotoAsync(href, new() { WaitUntil = WaitUntilState.NetworkIdle });

            var spec = new CaseSpecification();

            var dt = detailsPage.Locator("dl dt");
            var dd = detailsPage.Locator("dl dd");

            int count = await dt.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var labelRaw = (await dt.Nth(i).InnerTextAsync()).Trim();
                var value = (await dd.Nth(i).InnerTextAsync()).Trim();

                var label = labelRaw
                    .ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("–", "");

                if (label.Contains("gehäusetyp"))
                    spec.FormFactor = value;

                else if (label.Contains("volumen"))
                    spec.VolumeLiters = ParseDecimal(value);

                else if (label.Contains("abmessungen"))
                    spec.DimensionsMM = value;

                else if (label.Contains("grafikkarten"))
                    spec.MaxGPULengthMM = ParseMillimeters(value);

                else if (label.Contains("cpukühler"))
                    spec.MaxCoolerHeightMM = ParseMillimeters(value);

                else if (label.Contains("intern"))
                    ParseDriveBays(value, spec);

                else if (label.Contains("pcisteckplätze"))
                    spec.ExpansionSlots = value;

                else if (label.Contains("anschlüsse"))
                {
                    spec.HasUSB3 = value.Contains("usb-a 3", StringComparison.OrdinalIgnoreCase);
                    spec.HasUSBC = value.Contains("usb-c", StringComparison.OrdinalIgnoreCase);
                }

                else if (label.Contains("besonderheiten"))
                    spec.HasTemperedGlass =
                        value.Contains("glas", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("sichtfenster", StringComparison.OrdinalIgnoreCase);
            }

            return spec;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (detailsPage != null)
                await detailsPage.CloseAsync();
        }
    }
    private int? ParseMillimeters(string value)
    {
        var match = Regex.Match(value, @"(\d+)\s*mm");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }
    private decimal? ParseDecimal(string value)
    {
        var match = Regex.Match(value.Replace(",", "."), @"(\d+(\.\d+)?)");
        return match.Success ? decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;
    }
    private void ParseDriveBays(string value, CaseSpecification spec)
    {
        var bay25 = Regex.Match(value, @"(\d+)x\s*2\.5");
        var bay35 = Regex.Match(value, @"(\d+)x\s*3\.5");

        if (bay25.Success)
            spec.BayCount25 = int.Parse(bay25.Groups[1].Value);

        if (bay35.Success)
            spec.BayCount35 = int.Parse(bay35.Groups[1].Value);
    }
    private async Task<GPUSpecification?> ExtractGPUSpecsFromDetailsPageAsync(
    ILocator element,
    IPage currentPage)
    {
        IPage? detailsPage = null;

        try
        {
            var href = await element.Locator("a").First.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href))
                return null;

            if (!href.StartsWith("http"))
                href = "https://geizhals.de/" + href.TrimStart('/');

            detailsPage = await currentPage.Context.NewPageAsync();
            await detailsPage.GotoAsync(href, new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 15000
            });

            var spec = new GPUSpecification();

            var dt = detailsPage.Locator("dl dt");
            var dd = detailsPage.Locator("dl dd");

            int count = await dt.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var labelRaw = (await dt.Nth(i).InnerTextAsync()).Trim();
                var value = (await dd.Nth(i).InnerTextAsync()).Trim();

                var label = labelRaw
                    .ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("–", "");

                if (label.Contains("speicher"))
                    ParseGPUMemory(value, spec);

                else if (label.Contains("chipbezeichnung") || label.Contains("grafik"))
                    spec.Chipset = value;

                else if (label.Contains("tdp") || label.Contains("tgp"))
                    spec.TDP = ParseWatts(value);

                else if (label.Contains("stromanschlüsse"))
                    ParseGPUConnectors(value, spec);

                else if (label.Contains("abmessungen"))
                    ParseDimensions(value, spec);
            }

            return spec.MemorySize > 0 ? spec : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (detailsPage != null)
                await detailsPage.CloseAsync();
        }
    }
    private void ParseGPUMemory(string value, GPUSpecification spec)
    {
        // Example: "8GB GDDR6, 128bit ..."
        var sizeMatch = Regex.Match(value, @"(\d+)\s*gb", RegexOptions.IgnoreCase);
        if (sizeMatch.Success)
            spec.MemorySize = int.Parse(sizeMatch.Groups[1].Value);

        var typeMatch = Regex.Match(value, @"(gddr\d+x?)", RegexOptions.IgnoreCase);
        if (typeMatch.Success)
            spec.MemoryType = typeMatch.Groups[1].Value.ToUpperInvariant();
    }

    private int? ParseWatts(string value)
    {
        var match = Regex.Match(value, @"(\d+)\s*w", RegexOptions.IgnoreCase);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private void ParseGPUConnectors(string value, GPUSpecification spec)
    {
        var pin6 = Regex.Match(value, @"(\d+)x\s*6-pin", RegexOptions.IgnoreCase);
        if (pin6.Success)
            spec.Aux6PinCount = int.Parse(pin6.Groups[1].Value);

        var pin8 = Regex.Match(value, @"(\d+)x\s*8-pin", RegexOptions.IgnoreCase);
        if (pin8.Success)
            spec.Aux8PinCount = int.Parse(pin8.Groups[1].Value);
    }

    private void ParseDimensions(string value, GPUSpecification spec)
    {
        // Example: 205x112x45mm (LxBxH)
        var match = Regex.Match(value, @"(\d+)x(\d+)x(\d+)\s*mm");
        if (match.Success)
        {
            spec.LengthMM = int.Parse(match.Groups[1].Value);
            spec.WidthMM = int.Parse(match.Groups[2].Value);
            spec.HeightMM = int.Parse(match.Groups[3].Value);
        }
    }
    private async Task<CPUCoolerSpecification?> ExtractCoolerSpecsFromDetailsPageAsync(
    ILocator element,
    IPage currentPage)
    {
        IPage? detailsPage = null;

        try
        {
            var href = await element.Locator("a").First.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href))
                return null;

            if (!href.StartsWith("http"))
                href = "https://geizhals.de/" + href.TrimStart('/');

            detailsPage = await currentPage.Context.NewPageAsync();
            await detailsPage.GotoAsync(href, new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 15000
            });

            var spec = new CPUCoolerSpecification();

            var dt = detailsPage.Locator("dl dt");
            var dd = detailsPage.Locator("dl dd");

            int count = await dt.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var labelRaw = (await dt.Nth(i).InnerTextAsync()).Trim();
                var value = (await dd.Nth(i).InnerTextAsync()).Trim();

                var label = labelRaw
                    .ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("–", "");

                if (label.Contains("sockel"))
                    spec.SocketCompatibility = value;

                else if (label.Contains("tdp"))
                    spec.MaxTDP = ParseWatts(value);

                else if (label.Contains("abmessungen"))
                    spec.HeightMM = ParseHeight(value);

                else if (label.Contains("lüfter"))
                    spec.FanCount = ParseFanCount(value);

                else if (label.Contains("bauart"))
                    spec.IsLiquidCooled =
                        value.Contains("aio", StringComparison.OrdinalIgnoreCase) ||
                        value.Contains("wasser", StringComparison.OrdinalIgnoreCase);
            }

            return spec.SocketCompatibility != null ? spec : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (detailsPage != null)
                await detailsPage.CloseAsync();
        }
    }
    private int? ParseHeight(string value)
    {
        // Example: 101x33x101mm → height = 33
        var match = Regex.Match(value, @"\d+x(\d+)x\d+\s*mm");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private int? ParseFanCount(string value)
    {
        var match = Regex.Match(value, @"(\d+)x");
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private async Task<PSUSpecification?> ExtractPSUSpecsFromDetailsPageAsync(
    ILocator element,
    IPage currentPage)
    {
        IPage? detailsPage = null;

        try
        {
            var href = await element.Locator("a").First.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href))
                return null;

            if (!href.StartsWith("http"))
                href = "https://geizhals.de/" + href.TrimStart('/');

            detailsPage = await currentPage.Context.NewPageAsync();
            await detailsPage.GotoAsync(href, new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 15000
            });

            var spec = new PSUSpecification();

            var dt = detailsPage.Locator("dl dt");
            var dd = detailsPage.Locator("dl dd");

            int count = await dt.CountAsync();

            for (int i = 0; i < count; i++)
            {
                var labelRaw = (await dt.Nth(i).InnerTextAsync()).Trim();
                var value = (await dd.Nth(i).InnerTextAsync()).Trim();

                var label = labelRaw
                    .ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("-", "")
                    .Replace("–", "");

                if (label.Contains("kabelmanagement"))
                {
                    // fest / teilmodular / vollmodular
                    spec.Modular = !value.Contains("fest", StringComparison.OrdinalIgnoreCase);
                }
                else if (label.Contains("anschlüsse"))
                {
                    ParsePSUConnectors(value, spec);
                }
                else if (label.Contains("zertifikate"))
                {
                    spec.EfficiencyRating = Extract80Plus(value);
                }
                else if (label.Contains("abmessungen"))
                {
                    spec.DimensionsMM = value;
                }
                else if (label.Contains("formfaktor"))
                {
                    // optional, kept for future extension
                }
            }

            // Wattage is NOT a field — must be extracted from product name
            spec.Wattage = ExtractWattageFromTitle(
                await detailsPage.TitleAsync());

            return spec.Wattage > 0 ? spec : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (detailsPage != null)
                await detailsPage.CloseAsync();
        }
    }
    private int ExtractWattageFromTitle(string title)
    {
        var match = Regex.Match(title, @"(\d{3,4})\s*W", RegexOptions.IgnoreCase);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
    private string? Extract80Plus(string value)
    {
        if (value.Contains("platinum", StringComparison.OrdinalIgnoreCase))
            return "80 PLUS Platinum";
        if (value.Contains("gold", StringComparison.OrdinalIgnoreCase))
            return "80 PLUS Gold";
        if (value.Contains("silver", StringComparison.OrdinalIgnoreCase))
            return "80 PLUS Silver";
        if (value.Contains("bronze", StringComparison.OrdinalIgnoreCase))
            return "80 PLUS Bronze";
        if (value.Contains("80 plus", StringComparison.OrdinalIgnoreCase))
            return "80 PLUS";

        return null;
    }

    private void ParsePSUConnectors(string value, PSUSpecification spec)
    {

        var pcie6 = Regex.Match(value, @"(\d+)x\s*6-Pin", RegexOptions.IgnoreCase);
        if (pcie6.Success)
            spec.Aux6PinCount = int.Parse(pcie6.Groups[1].Value);

        var sata = Regex.Match(value, @"(\d+)x\s*SATA", RegexOptions.IgnoreCase);
        if (sata.Success)
            spec.SATAPowerCount = int.Parse(sata.Groups[1].Value);
    }

    private async Task<MotherboardSpec?> ExtractMotherboardSpecsFromDetailsPageAsync(
    ILocator element,
    IPage currentPage)
    {
        IPage? detailsPage = null;

        try
        {
            var linkElement = element.Locator("a").First;
            var href = await linkElement.GetAttributeAsync("href");

            if (string.IsNullOrEmpty(href))
            {
                _logger.LogWarning("⚠️ No href found in product element");
                return null;
            }

            if (!href.StartsWith("http"))
                href = "https://geizhals.de/" + href.TrimStart('/');

            _logger.LogDebug("📖 Opening motherboard details: {Url}", href);

            detailsPage = await currentPage.Context.NewPageAsync();
            await detailsPage.GotoAsync(
                href,
                new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 15000
                });

            var spec = new MotherboardSpec();

            var dtLocators = detailsPage.Locator("dl.fcols-data dt");
            var ddLocators = detailsPage.Locator("dl.fcols-data dd");

            int dtCount = await dtLocators.CountAsync();
            _logger.LogDebug("📋 Found {Count} specification fields", dtCount);

            if (dtCount == 0)
            {
                _logger.LogWarning("⚠️ No dl.fcols-data found, trying alternative selectors");

                dtLocators = detailsPage.Locator("dl.specs-grid dt");
                ddLocators = detailsPage.Locator("dl.specs-grid dd");
                dtCount = await dtLocators.CountAsync();

                _logger.LogDebug("📋 Found {Count} with specs-grid", dtCount);
            }

            for (int i = 0; i < dtCount; i++)
            {
                try
                {
                    var rawLabel = (await dtLocators.Nth(i).InnerTextAsync()).Trim();
                    var rawValue = (await ddLocators.Nth(i).InnerTextAsync()).Trim();

                    var label = rawLabel
                        .ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("–", "");

                    _logger.LogDebug("Field: {Label} = {Value}", rawLabel, rawValue);

                    if (label.Contains("sockel"))
                        spec.Socket = rawValue;

                    else if (label.Contains("chipsatz"))
                        spec.Chipset = rawValue;

                    else if (label.Contains("formfaktor"))
                        spec.FormFactor = NormalizeFormFactor(rawValue);

                    else if (label == "ram")
                        ParseRAMInfo(rawValue, spec);

                    else if (label.Contains("ramdatenrate"))
                        ParseMemorySpeed(rawValue, spec);

                    else if (label.Contains("anschlüsseinternstromversorgung"))
                        spec.PowerConnectors = rawValue;

                    else if (label.Contains("pcieslots"))
                        ParsePCIe(rawValue, spec);

                    else if (label.Contains("m.2slots"))
                        spec.M2SlotCount = CountByPrefix(rawValue, "M.2");

                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to extract field at index {Index}", i);
                }
            }

            _logger.LogInformation(
                "📊 Extracted Motherboard: Socket={Socket}, Chipset={Chipset}, FormFactor={FormFactor}, Slots={Slots}, Power={Power}",
                spec.Socket ?? "NULL",
                spec.Chipset ?? "NULL",
                spec.FormFactor ?? "NULL",
                spec.MemorySlots ?? 0,
                spec.PowerConnectors ?? "NULL");

            if (!string.IsNullOrEmpty(spec.Socket) || !string.IsNullOrEmpty(spec.Chipset))
                return spec;

            _logger.LogWarning("⚠️ Motherboard spec missing critical fields");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to extract motherboard specs");
            return null;
        }
        finally
        {
            if (detailsPage != null)
            {
                try { await detailsPage.CloseAsync(); }
                catch { }
            }
        }
    }

    private string? NormalizeFormFactor(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var upper = value.ToUpper();
        if (upper.Contains("µATX") || upper.Contains("MICRO"))
            return "Micro-ATX";
        if (upper.Contains("MINI"))
            return "Mini-ITX";
        if (upper.Contains("E-ATX"))
            return "E-ATX";
        if (upper.Contains("ATX"))
            return "ATX";

        return value;
    }

    private void ParseRAMInfo(string value, MotherboardSpec spec)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        // Slots + DDR type
        var slotMatch = Regex.Match(value, @"(\d+)\s*x\s*(DDR\d)", RegexOptions.IgnoreCase);
        if (slotMatch.Success)
        {
            spec.MemorySlots = int.Parse(slotMatch.Groups[1].Value);
            spec.MemoryType = slotMatch.Groups[2].Value.ToUpperInvariant();
        }

        // Max capacity
        var capMatch = Regex.Match(value, @"max\.\s*(\d+)\s*gb", RegexOptions.IgnoreCase);
        if (capMatch.Success)
            spec.MaxMemoryCapacityGB = int.Parse(capMatch.Groups[1].Value);
    }
    private void ParseMemorySpeed(string value, MotherboardSpec spec)
    {
        var match = Regex.Match(value, @"DDR\d[-–](\d+)");
        if (match.Success)
            spec.MaxMemorySpeedMHz = int.Parse(match.Groups[1].Value);
    }
    private void ParsePCIe(string value, MotherboardSpec spec)
    {
        spec.PCIeSlots = value;

        if (value.Contains("PCIe 5"))
            spec.MaxPCIeGeneration = "5.0";
        else if (value.Contains("PCIe 4"))
            spec.MaxPCIeGeneration = "4.0";
        else if (value.Contains("PCIe 3"))
            spec.MaxPCIeGeneration = "3.0";
    }
    private int CountByPrefix(string value, string keyword)
    {
        return Regex.Matches(value, keyword, RegexOptions.IgnoreCase).Count;
    }

    private void ExtractAndAttachSpecs(Component component, string name)
    {
        var specExtractor = _specExtractionService;

        switch (component.Type)
        {
            case ComponentType.CPU:
                component.CPUSpec = specExtractor.ExtractCPUSpec(name);
                if (component.CPUSpec != null)
                {
                
                }
                break;



            case ComponentType.RAM:
                component.RAMSpec = specExtractor.ExtractRAMSpec(name);
                if (component.RAMSpec != null)
                {
                    _logger.LogDebug("RAM Spec: {Capacity}GB {Type}-{Speed}, {Timings}",
                        component.RAMSpec.Capacity, component.RAMSpec.Type, component.RAMSpec.Speed, component.RAMSpec.Timings);
                }
                break;

            case ComponentType.SSD:
            case ComponentType.HDD:
                component.StorageSpec = specExtractor.ExtractStorageSpec(name);
                if (component.StorageSpec != null)
                {
                    _logger.LogDebug("Storage Spec: {Capacity}GB {Type}",
                        component.StorageSpec.Capacity, component.StorageSpec.Type);
                }
                break;

        }
    }

    private async Task<Component?> ParseProductElementAsync(ILocator element, ComponentCategory category)
    {
        try
        {
            string? name = null;
            var nameSelectors = new[]
            {
            "h3 a",
            ".product-name",
            "a[href*='.html']"
        };

            foreach (var selector in nameSelectors)
            {
                try
                {
                    var nameElement = element.Locator(selector).First;
                    if (await nameElement.CountAsync() > 0)
                    {
                        name = await nameElement.TextContentAsync();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogDebug("❌ Failed to extract name");
                return null;
            }

            _logger.LogDebug("✅ Found name: {Name}", name.Substring(0, Math.Min(50, name.Length)));

            string? priceText = null;
            var priceSelectors = new[]
            {
            ".price",
            ".galleryview__price-link .price",
            "span.price",
            "[class*='price']"
        };

            foreach (var selector in priceSelectors)
            {
                try
                {
                    var priceElement = element.Locator(selector).First;
                    if (await priceElement.CountAsync() > 0)
                    {
                        priceText = await priceElement.TextContentAsync();
                        if (!string.IsNullOrWhiteSpace(priceText))
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(priceText))
            {
                _logger.LogDebug("❌ Failed to extract price for: {Name}", name.Substring(0, Math.Min(30, name.Length)));
                return null;
            }

            _logger.LogDebug("✅ Found price text: {Price}", priceText);

            priceText = priceText.Replace("ab", "").Replace("€", "").Replace("EUR", "")
                .Replace(".", "").Replace(",", ".").Trim();

            if (!decimal.TryParse(priceText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                _logger.LogDebug("❌ Failed to parse price: {PriceText}", priceText);
                return null;
            }

            _logger.LogDebug("✅ Parsed price: {Price}", price);

            // Check price filtering
            if (!IsWithinPriceRange(price, category))
            {
                _logger.LogDebug("❌ Price {Price} outside range for {Category}", price, category);
                return null;
            }

            _logger.LogDebug("✅ Price within range: {Price}", price);

            string? imageUrl = null;
            try
            {
                var imgElement = element.Locator("img").First;
                if (await imgElement.CountAsync() > 0)
                {
                    imageUrl = await imgElement.GetAttributeAsync("src") ??
                              await imgElement.GetAttributeAsync("data-src");
                }
            }
            catch
            {
                // Image is optional
            }

            if (!string.IsNullOrWhiteSpace(imageUrl) && !imageUrl.StartsWith("http"))
            {
                imageUrl = imageUrl.StartsWith("//")
                    ? "https:" + imageUrl
                    : "https://gzhls.at" + imageUrl;
            }

            var component = new Component
            {
                Name = CleanProductName(name),
                Type = MapCategoryToComponentType(category),
                Manufacturer = DetermineManufacturer(name),
                ImageUrl = imageUrl,
                LowestPrice = price,
                AveragePrice = price,
                OfferCount = 0,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Successfully parsed: {Name} - {Price}", component.Name, component.LowestPrice);
            return component;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse product element");
            return null;
        }
    }

    private bool IsWithinPriceRange(decimal price, ComponentCategory category)
    {
        if (!PriceRanges.TryGetValue(category, out var range))
            return true; 

        return price >= range.Min && price <= range.Max;
    }

    private List<Component> FilterByQuality(
     List<Component> components,
     ComponentCategory category,
     int maxResults)
    {
        return components
            .Where(c => c.Rating == null || c.Rating >= 3.0m)
            .OrderByDescending(c => c.QualityScore)
            .ThenByDescending(c => c.ReviewCount)
            .ThenBy(c => c.LowestPrice ?? decimal.MaxValue)
            .Take(maxResults)
            .ToList();
    }

    private async Task ExtractListPageQualitySignalsAsync(
      ILocator item,
      Component component)
    {
        // ---------- Rating (CSS variable --stars-rating) ----------
        var ratingNode = item.Locator(".stars-rating").First;
        if (await ratingNode.CountAsync() > 0)
        {
            var style = await ratingNode.GetAttributeAsync("style");
            if (!string.IsNullOrEmpty(style))
            {
                var match = Regex.Match(style, @"--stars-rating:\s*([\d.]+)");
                if (match.Success &&
                    decimal.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var rating))
                {
                    component.Rating = rating;
                }
            }
        }

        // ---------- Review count ("6 Bewertungen") ----------
        var reviewNode = item.Locator(".stars-rating-label-bottom").First;
        if (await reviewNode.CountAsync() > 0)
        {
            var text = await reviewNode.InnerTextAsync();
            component.ReviewCount = ParseFirstInt(text);
        }

        // ---------- Offer count ("2 Angebote") ----------
        var offerNode = item.Locator(".galleryview__offercount-link").First;
        if (await offerNode.CountAsync() > 0)
        {
            var text = await offerNode.InnerTextAsync();
            component.OfferCount = ParseFirstInt(text);
        }

        // ---------- Price (lowest price on list page) ----------
        var priceNode = item.Locator(".price").First;
        if (await priceNode.CountAsync() > 0)
        {
            var text = await priceNode.InnerTextAsync();
            component.LowestPrice = ParsePrice(text);
        }
    }
    private int ParseFirstInt(string text)
    {
        var match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    private decimal? ParsePrice(string text)
    {
        // "€ 10,90" → 10.90
        var cleaned = text
            .Replace("€", "")
            .Replace(".", "")
            .Replace(",", ".")
            .Trim();

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
            ? price
            : null;
    }

    private bool ShouldScrapeDetails(Component c)
    {
        // Absolute blockers
        if (c.ReviewCount < 3)
            return false;

        if (c.Rating.HasValue && c.Rating < 3.5m)
            return false;

        // Score-based decision
        return c.QualityScore >= 60;
    }

    private double CalculateQualityScore(Component c)
    {
        double score = 0;

        if (c.Rating.HasValue)
            score += (double)c.Rating.Value * 20;   // max 100

        score += Math.Min(c.ReviewCount, 200) * 0.2; // trust via volume
        score += Math.Min(c.OfferCount, 50) * 0.5;   // market availability

        return Math.Round(score, 2);
    }

    private string CleanProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;

        var cleanName = name.Trim();

        cleanName = System.Text.RegularExpressions.Regex.Replace(
            cleanName,
            @",?\s*(boxed\s*(ohne|mit)?\s*Kühler|box|ohne\s*Kühler|mit\s*Kühler)\s*(\(.*?\))?$",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        ).Trim();

        cleanName = System.Text.RegularExpressions.Regex.Replace(
            cleanName,
            @"\s+\([A-Z0-9\-]+\)$",
            ""
        ).Trim();

        cleanName = System.Text.RegularExpressions.Regex.Replace(cleanName, @"\s+", " ");

        return cleanName;
    }

    private ComponentType MapCategoryToComponentType(ComponentCategory category)
    {
        return category switch
        {
            ComponentCategory.GPU => ComponentType.GPU,
            ComponentCategory.CPU => ComponentType.CPU,
            ComponentCategory.Motherboard => ComponentType.Motherboard,
            ComponentCategory.RAM => ComponentType.RAM,
            ComponentCategory.SSD => ComponentType.SSD,
            ComponentCategory.PSU => ComponentType.PSU,
            ComponentCategory.Case => ComponentType.Case,
            ComponentCategory.Cooling => ComponentType.Cooling,
            _ => ComponentType.Other
        };
    }

    private static Manufacturer DetermineManufacturer(string name)
    {
        var nameLower = name.ToLowerInvariant();

        if (nameLower.Contains("intel")) return Manufacturer.Intel;
        if (nameLower.Contains("amd")) return Manufacturer.AMD;
        if (nameLower.Contains("nvidia") || nameLower.Contains("geforce")) return Manufacturer.NVIDIA;
        if (nameLower.Contains("asus")) return Manufacturer.ASUS;
        if (nameLower.Contains("msi")) return Manufacturer.MSI;
        if (nameLower.Contains("gigabyte")) return Manufacturer.Gigabyte;
        if (nameLower.Contains("asrock")) return Manufacturer.ASRock;
        if (nameLower.Contains("corsair")) return Manufacturer.Corsair;
        if (nameLower.Contains("gskill") || nameLower.Contains("g.skill")) return Manufacturer.GSkill;
        if (nameLower.Contains("samsung")) return Manufacturer.Samsung;
        if (nameLower.Contains("crucial")) return Manufacturer.Crucial;
        if (nameLower.Contains("western digital") || nameLower.Contains("wd ")) return Manufacturer.WesternDigital;
        if (nameLower.Contains("seagate")) return Manufacturer.Seagate;
        if (nameLower.Contains("nzxt")) return Manufacturer.NZXT;
        if (nameLower.Contains("cooler master")) return Manufacturer.CoolerMaster;
        if (nameLower.Contains("be quiet") || nameLower.Contains("bequiet")) return Manufacturer.BeQuiet;
        if (nameLower.Contains("fractal")) return Manufacturer.Fractal;
        if (nameLower.Contains("thermaltake")) return Manufacturer.Thermaltake;
        if (nameLower.Contains("seasonic")) return Manufacturer.Seasonic;
        if (nameLower.Contains("evga")) return Manufacturer.EVGA;

        return Manufacturer.Other;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }
}

public class PriceRange
{
    public decimal Min { get; }
    public decimal Max { get; }

    public PriceRange(decimal min, decimal max)
    {
        Min = min;
        Max = max;
    }
}
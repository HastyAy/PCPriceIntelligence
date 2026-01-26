using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using web.Services;

namespace web.Components.Pages;

public partial class BuildConfigurator
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "preset")]
    public string? PresetSessionId { get; set; }

    private const string BuildSessionKey = "current-pc-build";

    // =========================
    // PARAMETERS
    // =========================
    [Parameter] public int? BuildId { get; set; }
    private bool IsEditMode => BuildId.HasValue;

    // =========================
    // UI STATE
    // =========================
    private bool showBrowser;
    private bool isLoadingComponents;
    private bool isChecking;
    private bool isAIChecking;
    private bool isSaving;
    private bool showSaveDialog;
    private bool showSuccessMessage;
    private string? successMessage;

    private ComponentType currentBrowsingType;

    private string searchQuery = string.Empty;
    private string? manufacturerFilter;
    private string sortOption = "quality";

    private string buildName = string.Empty;
    private string buildNotes = string.Empty;

    // =========================
    // DATA
    // =========================
    private List<Component> availableComponents = new();
    private List<Component> filteredComponents = new();
    private Dictionary<ComponentType, Component> selectedComponents = new();

    private CompatibilityResult? compatibilityResult;
    private AIBuildAnalysis? aiAnalysisResult;

    // =========================
    // CONFIG
    // =========================
    private readonly List<ComponentType> componentTypes = new()
    {
        ComponentType.CPU,
        ComponentType.Motherboard,
        ComponentType.RAM,
        ComponentType.GPU,
        ComponentType.PSU,
        ComponentType.Case,
        ComponentType.Cooling,
        ComponentType.SSD
    };

    // =========================
    // DERIVED VALUES
    // =========================
    private decimal TotalPrice =>
        selectedComponents.Values.Sum(c => c.LowestPrice ?? 0);

    private bool HasMinimumComponents =>
        selectedComponents.ContainsKey(ComponentType.CPU) &&
        selectedComponents.ContainsKey(ComponentType.Motherboard) &&
        selectedComponents.ContainsKey(ComponentType.RAM) &&
        selectedComponents.ContainsKey(ComponentType.PSU);

    private int EstimatedWattage
    {
        get
        {
            int wattage = 100; // Base system

            if (selectedComponents.TryGetValue(ComponentType.CPU, out var cpu) && cpu.CPUSpec != null)
                wattage += cpu.CPUSpec.TDP;

            if (selectedComponents.TryGetValue(ComponentType.GPU, out var gpu) && gpu.GPUSpec?.TDP != null)
                wattage += gpu.GPUSpec.TDP.Value;

            return wattage;
        }
    }

    // =========================
    // LIFECYCLE
    // =========================
    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            await LoadExistingBuild();
        }
        else if (!string.IsNullOrEmpty(PresetSessionId))
        {
            await LoadPresetBuild();
        }
    }
    private async Task LoadPresetBuild()
    {
        var components = await ComponentService.GetPrebuiltConfigurationAsync(PresetSessionId!);
        if (components.Any())
        {
            selectedComponents = components;
            await PersistBuildSessionAsync();

            showSuccessMessage = true;
            successMessage = "Build configured! Review and customize your components.";
        }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || IsEditMode || !string.IsNullOrEmpty(PresetSessionId)) return;

        var dto = await JS.InvokeAsync<BuildSessionDto?>("buildStorage.load", BuildSessionKey);
        if (dto == null || !dto.ComponentIds.Any()) return;

        // Load all at once instead of one by one
        var ids = dto.ComponentIds.Values.ToList();

        foreach (var (type, id) in dto.ComponentIds)
        {
            var component = await ComponentService.GetComponentByIdAsync(id);
            if (component != null)
                selectedComponents[type] = component;
        }

        StateHasChanged();
    }

    private async Task LoadExistingBuild()
    {
        var build = await BuildService.GetBuildByIdAsync(BuildId!.Value);
        if (build != null)
        {
            buildName = build.Name;
            buildNotes = build.Notes ?? string.Empty;

            var componentIds = JsonSerializer.Deserialize<Dictionary<string, int>>(build.ComponentsJson);
            if (componentIds != null)
            {
                foreach (var kvp in componentIds)
                {
                    if (Enum.TryParse<ComponentType>(kvp.Key, out var type))
                    {
                        var component = await ComponentService.GetComponentByIdAsync(kvp.Value);
                        if (component != null)
                            selectedComponents[type] = component;
                    }
                }
            }
        }
    }

    // =========================
    // SESSION PERSISTENCE
    // =========================
    private async Task PersistBuildSessionAsync()
    {
        var dto = new BuildSessionDto
        {
            ComponentIds = selectedComponents.ToDictionary(k => k.Key, v => v.Value.Id)
        };
        await JS.InvokeVoidAsync("buildStorage.save", BuildSessionKey, dto);
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated != true)
            return null;

        return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    // =========================
    // BROWSER LOGIC
    // =========================
    private async Task OpenBrowser(ComponentType type)
    {
        currentBrowsingType = type;
        showBrowser = true;
        isLoadingComponents = true;

        availableComponents = await ComponentService.GetComponentsByCategoryAsync(type, 200);
        ApplyFilters();

        isLoadingComponents = false;
    }

    private void CloseBrowser()
    {
        showBrowser = false;
        searchQuery = string.Empty;
        manufacturerFilter = null;
    }

    private async Task SelectComponent(Component component)
    {
        var full = await ComponentService.GetComponentByIdAsync(component.Id);
        if (full != null)
        {
            selectedComponents[currentBrowsingType] = full;
            await PersistBuildSessionAsync();

            // Clear previous results when components change
            compatibilityResult = null;
            aiAnalysisResult = null;
        }
        CloseBrowser();
    }

    private async Task RemoveComponent(ComponentType type)
    {
        selectedComponents.Remove(type);
        await PersistBuildSessionAsync();

        // Clear results
        compatibilityResult = null;
        aiAnalysisResult = null;
    }

    // =========================
    // FILTERING / SORTING
    // =========================
    private void ApplyFilters()
    {
        filteredComponents = availableComponents
            .Where(c =>
                (string.IsNullOrWhiteSpace(searchQuery) ||
                 c.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) &&
                (manufacturerFilter == null ||
                 c.Manufacturer.ToString() == manufacturerFilter))
            .OrderByDescending(GetSortScore)
            .ToList();
    }

    private double GetSortScore(Component c)
    {
        return sortOption switch
        {
            "rating" => (double)(c.Rating ?? 0) * Math.Log10(c.ReviewCount + 1),
            "offers" => c.OfferCount,
            "price" => -(double)(c.LowestPrice ?? decimal.MaxValue),
            _ => c.QualityScore
        };
    }

    private IEnumerable<string> GetAvailableManufacturers()
    {
        return availableComponents
            .Select(c => c.Manufacturer.ToString())
            .Distinct()
            .OrderBy(x => x);
    }

    private string GetTypeLabel(ComponentType type) => type switch
    {
        ComponentType.CPU => "CPU",
        ComponentType.GPU => "Graphics Card",
        ComponentType.RAM => "Memory",
        ComponentType.Motherboard => "Motherboard",
        ComponentType.PSU => "Power Supply",
        ComponentType.Case => "Case",
        ComponentType.Cooling => "CPU Cooler",
        ComponentType.SSD => "Storage",
        ComponentType.HDD => "Hard Drive",
        _ => type.ToString()
    };

    private string GetScoreColor(int score) => score switch
    {
        >= 80 => "text-success",
        >= 60 => "text-warning",
        _ => "text-danger"
    };

    // =========================
    // COMPATIBILITY CHECKS
    // =========================
    private async Task CheckCompatibility()
    {
        isChecking = true;
        try
        {
            compatibilityResult = compatibilityService.RunPreCompatibilityChecks(selectedComponents);
        }
        finally
        {
            isChecking = false;
        }
    }

    private async Task AskAIForAnalysis()
    {
        isAIChecking = true;
        try
        {
            var componentInfos = selectedComponents.Values.Select(c => new ComponentInfo
            {
                Type = c.Type.ToString(),
                Name = c.Name,
                Price = c.LowestPrice ?? 0,
                TDP = GetComponentTDP(c),
                Socket = GetComponentSocket(c)
            }).ToList();

            aiAnalysisResult = await GeminiService.AnalyzeBuildAsync(componentInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI analysis");
            // Show error to user
            aiAnalysisResult = new AIBuildAnalysis
            {
                OverallScore = 0,
                Summary = "Unable to get AI analysis. Please try again later.",
                IsCompatible = false,
                CompatibilityIssues = new List<string> { "AI service unavailable" }
            };
        }
        finally
        {
            isAIChecking = false;
        }
    }

    private int? GetComponentTDP(Component c)
    {
        if (c.CPUSpec != null) return c.CPUSpec.TDP;
        if (c.GPUSpec?.TDP != null) return c.GPUSpec.TDP;
        return null;
    }

    private string? GetComponentSocket(Component c)
    {
        if (c.MotherboardSpec != null) return c.MotherboardSpec.Socket;
        if (c.CPUCoolerSpec != null) return c.CPUCoolerSpec.SocketCompatibility;
        return null;
    }

    // =========================
    // BUILD ACTIONS
    // =========================
    private void OpenSaveDialog()
    {
        showSaveDialog = true;
        showSuccessMessage = false;
    }

    private void CloseSaveDialog()
    {
        showSaveDialog = false;
    }

    private async Task ClearBuild()
    {
        selectedComponents.Clear();
        compatibilityResult = null;
        aiAnalysisResult = null;
        buildName = string.Empty;
        buildNotes = string.Empty;

        await JS.InvokeVoidAsync("buildStorage.clear", BuildSessionKey);
    }

    private async Task SaveBuild()
    {
        if (string.IsNullOrWhiteSpace(buildName)) return;

        isSaving = true;
        try
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                throw new InvalidOperationException("User not authenticated");

            if (IsEditMode)
            {
                var build = await BuildService.GetBuildByIdAsync(BuildId!.Value);
                if (build != null)
                {
                    build.Name = buildName;
                    build.Notes = buildNotes;
                    build.TotalPrice = TotalPrice;
                    build.ComponentsJson = SerializeSelectedComponents();
                    await BuildService.UpdateBuildAsync(build);
                }
            }
            else
            {
                var build = new PCBuild
                {
                    UserId = userId,
                    Name = buildName,
                    Notes = buildNotes,
                    TotalPrice = TotalPrice,
                    ComponentsJson = SerializeSelectedComponents(),
                    IsPublic = false
                };
                await BuildService.SaveBuildAsync(build);
            }

            showSaveDialog = false;
            showSuccessMessage = true;
            successMessage = IsEditMode ? "Build updated successfully!" : "Build saved successfully!";

            // Clear session storage after successful save
            if (!IsEditMode)
            {
                await JS.InvokeVoidAsync("buildStorage.clear", BuildSessionKey);
            }
        }
        finally
        {
            isSaving = false;
        }
    }

    private string SerializeSelectedComponents()
    {
        var map = selectedComponents.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => kvp.Value.Id
        );
        return JsonSerializer.Serialize(map);
    }

    // =========================
    // DTOs
    // =========================
    public class BuildSessionDto
    {
        public Dictionary<ComponentType, int> ComponentIds { get; set; } = new();
    }
}
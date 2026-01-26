using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using web.Services;

namespace web.Components.Pages
{
    public partial class BuildDetails
    {
        [Parameter]
        public int BuildId { get; set; }

        private PCBuild? build;
        private Dictionary<ComponentType, Component> components = new();
        private bool isLoading = true;
        private bool showDeleteConfirm = false;
        private bool isDeleting = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadBuild();
        }

        private async Task LoadBuild()
        {
            isLoading = true;
            try
            {
                build = await BuildService.GetBuildByIdAsync(BuildId);

                if (build != null)
                {
                    components = await BuildService.GetBuildComponentsAsync(build);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading build: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private string GetTypeLabel(ComponentType type)
        {
            return type switch
            {
                ComponentType.GPU => "Graphics Card (GPU)",
                ComponentType.CPU => "Processor (CPU)",
                ComponentType.Motherboard => "Motherboard",
                ComponentType.RAM => "Memory (RAM)",
                ComponentType.PSU => "Power Supply (PSU)",
                ComponentType.SSD => "Storage (SSD)",
                ComponentType.HDD => "Storage (HDD)",
                ComponentType.Case => "Case",
                ComponentType.Cooling => "CPU Cooler",
                _ => type.ToString()
            };
        }

        private string GetSpecSummary(ComponentType type, Component component)
        {
            return type switch
            {
                ComponentType.CPU when component.CPUSpec != null =>
                    $"{component.CPUSpec.Cores}C/{component.CPUSpec.Threads}T • {component.CPUSpec.BaseClock}GHz • {component.CPUSpec.TDP}W TDP",

                ComponentType.GPU when component.GPUSpec != null =>
                    $"{component.GPUSpec.MemorySize}GB {component.GPUSpec.MemoryType} • {component.GPUSpec.Chipset ?? "N/A"} • {component.GPUSpec.TDP}W",

                ComponentType.RAM when component.RAMSpec != null =>
                    $"{component.RAMSpec.Capacity}GB ({component.RAMSpec.ModuleCount}x) • {component.RAMSpec.Type} • {component.RAMSpec.Speed}MHz",

                ComponentType.Motherboard when component.MotherboardSpec != null =>
                    $"{component.MotherboardSpec.Socket} • {component.MotherboardSpec.Chipset} • {component.MotherboardSpec.FormFactor}",

                ComponentType.PSU when component.PSUSpec != null =>
                    $"{component.PSUSpec.Wattage}W • {component.PSUSpec.EfficiencyRating ?? "N/A"} • {(component.PSUSpec.Modular ? "Modular" : "Non-Modular")}",

                ComponentType.SSD or ComponentType.HDD when component.StorageSpec != null =>
                    $"{component.StorageSpec.Capacity}GB • {component.StorageSpec.Type} • {component.StorageSpec.Interface}",

                ComponentType.Case when component.CaseSpec != null =>
                    $"{component.CaseSpec.FormFactor} • Max GPU: {component.CaseSpec.MaxGPULengthMM}mm",

                ComponentType.Cooling when component.CPUCoolerSpec != null =>
                    $"{(component.CPUCoolerSpec.IsLiquidCooled ? "Liquid" : "Air")} • {component.CPUCoolerSpec.MaxTDP}W TDP • {component.CPUCoolerSpec.HeightMM}mm",

                _ => string.Empty
            };
        }

        private void EditBuild()
        {
            Navigation.NavigateTo($"/build/edit/{BuildId}");
        }

        private void ConfirmDelete()
        {
            showDeleteConfirm = true;
        }

        private async Task DeleteBuild()
        {
            isDeleting = true;
            try
            {
                await BuildService.DeleteBuildAsync(BuildId);
                Navigation.NavigateTo("/my-builds");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting build: {ex.Message}");
                showDeleteConfirm = false;
            }
            finally
            {
                isDeleting = false;
            }
        }
    }
}
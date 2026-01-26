using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace web.Components.Pages
{
    public partial class Components
    {
        private List<Component> allComponents = new();
        private List<Component> filteredComponents = new();
        private bool isLoading = true;
        private string searchTerm = "";
        private ComponentType? selectedType;
        private Manufacturer? selectedManufacturer;

        protected override async Task OnInitializedAsync()
        {
            await LoadComponents();
        }

        private async Task LoadComponents()
        {
            isLoading = true;
            allComponents = await DbContext.Components
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ApplyFilters();
            isLoading = false;
        }

        private void FilterByType(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value?.ToString()))
            {
                selectedType = null;
            }
            else
            {
                selectedType = Enum.Parse<ComponentType>(e.Value.ToString()!);
            }
            ApplyFilters();
        }

        private void FilterByManufacturer(ChangeEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Value?.ToString()))
            {
                selectedManufacturer = null;
            }
            else
            {
                selectedManufacturer = Enum.Parse<Manufacturer>(e.Value.ToString()!);
            }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            filteredComponents = allComponents.AsQueryable().ToList();

            if (selectedType.HasValue)
            {
                filteredComponents = filteredComponents.Where(c => c.Type == selectedType.Value).ToList();
            }

            if (selectedManufacturer.HasValue)
            {
                filteredComponents = filteredComponents.Where(c => c.Manufacturer == selectedManufacturer.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredComponents = filteredComponents
                    .Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }
    }
}
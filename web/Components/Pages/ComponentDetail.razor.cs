using Domain.Entities;
using Microsoft.AspNetCore.Components;
using web.Services;

namespace web.Components.Pages
{
    public partial class ComponentDetail
    {
        [Parameter]
        public int Id { get; set; }

        private Component? component;
        private bool isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadComponent();
        }

        protected override async Task OnParametersSetAsync()
        {
            await LoadComponent();
        }

        private async Task LoadComponent()
        {
            isLoading = true;
            try
            {
                component = await ComponentService.GetComponentByIdAsync(Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading component: {ex.Message}");
                component = null;
            }
            finally
            {
                isLoading = false;
            }
        }

        private void GoBack()
        {
            Navigation.NavigateTo("/build/new");
        }
    }
}
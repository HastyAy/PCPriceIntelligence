using System.Security.Claims;
using Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using web.Services;

namespace web.Components.Pages
{
    public partial class MyBuilds
    {
        private List<PCBuild> builds = new();
        private bool isLoading = true;
        private bool showDeleteConfirm = false;
        private bool isDeleting = false;
        private PCBuild? buildToDelete;
        private string? userId;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);

            await LoadBuilds();
        }

        private async Task LoadBuilds()
        {
            isLoading = true;
            try
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    builds = await BuildService.GetUserBuildsAsync(userId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading builds: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ViewBuild(int buildId)
        {
            Navigation.NavigateTo($"/build/{buildId}");
        }

        private void ConfirmDelete(PCBuild build)
        {
            buildToDelete = build;
            showDeleteConfirm = true;
        }

        private void CancelDelete()
        {
            buildToDelete = null;
            showDeleteConfirm = false;
        }

        private async Task DeleteBuild()
        {
            if (buildToDelete == null) return;

            isDeleting = true;
            try
            {
                await BuildService.DeleteBuildAsync(buildToDelete.Id);
                builds.Remove(buildToDelete);
                CancelDelete();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting build: {ex.Message}");
            }
            finally
            {
                isDeleting = false;
            }
        }
    }
}
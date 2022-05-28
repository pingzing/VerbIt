using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace VerbIt.Client.Pages.Dashboard
{
    public partial class DashboardLayout : LayoutComponentBase
    {
        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [CascadingParameter]
        protected Task<AuthenticationState> AuthState { get; set; } = null!;

        protected async override Task OnInitializedAsync()
        {
            base.OnInitialized();
            ClaimsPrincipal user = (await AuthState).User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                NavManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(NavManager.Uri)}");
            }
        }
    }
}

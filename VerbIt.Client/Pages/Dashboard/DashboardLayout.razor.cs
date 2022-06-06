using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
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
            await ValidateAuthExpiry();
        }

        private async Task ValidateAuthExpiry()
        {
            Console.WriteLine($"Validating auth Expiry via Dashboard Layout's OnInit.");
            ClaimsPrincipal user = (await AuthState).User;
            Claim? expiryClaim = user?.Claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp);
            if (expiryClaim == null)
            {
                NavManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(NavManager.Uri)}");
                return;
            }

            var expiryTime = DateTimeOffset.FromUnixTimeSeconds(int.Parse(expiryClaim.Value));
            if (expiryTime < DateTimeOffset.UtcNow)
            {
                NavManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(NavManager.Uri)}");
            }
        }
    }
}

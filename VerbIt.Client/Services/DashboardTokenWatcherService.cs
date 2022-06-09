using Microsoft.AspNetCore.Components;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VerbIt.Client.Authentication;

namespace VerbIt.Client.Services
{
    public class DashboardTokenWatcherService
    {
        private readonly NavigationManager _navManager;
        private readonly INetworkService _networkService;
        private readonly JwtAuthStateProvider _jwtAuthStateProvider;

        public DashboardTokenWatcherService(
            INetworkService networkService,
            JwtAuthStateProvider jwtAuthStateProvider,
            NavigationManager navManager
        )
        {
            _networkService = networkService;
            _jwtAuthStateProvider = jwtAuthStateProvider;
            _navManager = navManager;
        }

        // Called by the root router's OnNavigateAsync method
        public async void OnNavigated(string newPath)
        {
            if (newPath.StartsWith("dashboard"))
            {
                Console.WriteLine($"Validating token expiry in Dashboard...");
                ClaimsPrincipal user = (await _jwtAuthStateProvider.GetAuthenticationStateAsync()).User;
                Claim? expiryClaim = user?.Claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp);
                if (expiryClaim == null)
                {
                    Console.WriteLine($"Expiry is null, logging out.");
                    await _networkService.Logout();
                    _navManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(_navManager.Uri)}");
                    return;
                }

                var expiryTime = DateTimeOffset.FromUnixTimeSeconds(int.Parse(expiryClaim.Value));
                if (expiryTime < DateTimeOffset.UtcNow)
                {
                    Console.WriteLine($"Claim token is expired, logging out.");
                    await _networkService.Logout();
                    _navManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(_navManager.Uri)}");
                }
            }
        }
    }
}

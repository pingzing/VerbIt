using Microsoft.AspNetCore.Components;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VerbIt.Client.Authentication;

namespace VerbIt.Client.Services
{
    // Responsible for listening for all navigation events, and redirecting the user to the login page
    // if their credentials have expired.
    public class DashboardTokenWatcherService
    {
        private readonly NavigationManager _navManager;
        private readonly INetworkService _networkService;
        private readonly JwtAuthStateProvider _jwtAuthStateProvider;
        private readonly ILogger<DashboardTokenWatcherService> _logger;

        public DashboardTokenWatcherService(
            INetworkService networkService,
            JwtAuthStateProvider jwtAuthStateProvider,
            NavigationManager navManager,
            ILogger<DashboardTokenWatcherService> logger
        )
        {
            _networkService = networkService;
            _jwtAuthStateProvider = jwtAuthStateProvider;
            _navManager = navManager;
            _logger = logger;
        }

        // Called by the root router's OnNavigateAsync method
        public async void OnNavigated(string newPath)
        {
            if (newPath.StartsWith("dashboard"))
            {
                _logger.LogDebug($"Validating token expiry in Dashboard...");
                ClaimsPrincipal user = (await _jwtAuthStateProvider.GetAuthenticationStateAsync()).User;
                Claim? expiryClaim = user?.Claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp);
                if (expiryClaim == null)
                {
                    _logger.LogDebug($"Expiry is null, logging out.");
                    await _networkService.Logout();
                    _navManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(_navManager.Uri)}");
                    return;
                }

                var expiryTime = DateTimeOffset.FromUnixTimeSeconds(int.Parse(expiryClaim.Value));
                if (expiryTime < DateTimeOffset.UtcNow)
                {
                    _logger.LogDebug($"Claim token is expired, logging out.");
                    await _networkService.Logout();
                    _navManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(_navManager.Uri)}");
                }
            }
        }
    }
}

using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace VerbIt.Client.Authentication;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var anonymous = new ClaimsIdentity();
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(anonymous)));
    }
}

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using VerbIt.Client.Services;

namespace VerbIt.Client.Authentication;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    public const string AuthTokenKey = "authToken";

    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorageService;
    private AuthenticationState _unauthenticated;

    public JwtAuthStateProvider(HttpClient httpClient, ILocalStorageService localStorageService)
    {
        _httpClient = httpClient;
        _localStorageService = localStorageService;
        _unauthenticated = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string jwt = await _localStorageService.GetItemAsync<string>(AuthTokenKey);
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return _unauthenticated;
        }
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", jwt);

        var authState = new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(JwtParser.ParseClaims(jwt), "jwtAuthType"))
        );

        return authState;
    }

    public void NotifyUserAuthentication(string jwt)
    {
        ClaimsPrincipal authenticatedUserClaim = new ClaimsPrincipal(
            new ClaimsIdentity(JwtParser.ParseClaims(jwt), "jwtAuthType")
        );

        Task<AuthenticationState> newAuthState = Task.FromResult(new AuthenticationState(authenticatedUserClaim));
        NotifyAuthenticationStateChanged(newAuthState);
    }

    public void NotifyUserLogout()
    {
        Task<AuthenticationState> newAuthState = Task.FromResult(_unauthenticated);
        NotifyAuthenticationStateChanged(newAuthState);
    }
}

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace VerbIt.Client.Authentication;

// This is used to power the <AuthorizeView> component.
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

        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaims(jwt), "jwtAuthType")));

        return authState;
    }

    public void NotifyUserAuthentication(string jwt)
    {
        ClaimsPrincipal authenticatedUserClaim = new ClaimsPrincipal(new ClaimsIdentity(ParseClaims(jwt), "jwtAuthType"));

        Task<AuthenticationState> newAuthState = Task.FromResult(new AuthenticationState(authenticatedUserClaim));
        NotifyAuthenticationStateChanged(newAuthState);
    }

    public void NotifyUserLogout()
    {
        Task<AuthenticationState> newAuthState = Task.FromResult(_unauthenticated);
        NotifyAuthenticationStateChanged(newAuthState);
    }

    private static IEnumerable<Claim> ParseClaims(string jwt)
    {
        string? payload = jwt.Split('.')[1];
        if (payload == null)
        {
            return Array.Empty<Claim>();
        }

        byte[] jsonBytes = ParseBase64WithoutPadding(payload);

        Dictionary<string, object>? keyPairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)!;
        return keyPairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!)).ToList();
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        // Add padding characters if they're missing.
        base64 += (base64.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => "",
        };

        return Convert.FromBase64String(base64);
    }
}

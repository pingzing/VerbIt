using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VerbIt.Client.Authentication;
using VerbIt.DataModels;

namespace VerbIt.Client.Services;

public class NetworkService : INetworkService
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly HttpClient _httpClient;
    private readonly JwtAuthStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navManager;

    public NetworkService(
        HttpClient httpClient,
        JwtAuthStateProvider authStateProvider,
        ILocalStorageService localStorageService,
        NavigationManager navManager
    )
    {
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorageService = localStorageService;
        _navManager = navManager;
    }

    public async Task<bool> Login(LoginRequest request, CancellationToken token)
    {
        HttpResponseMessage result = await _httpClient.PostAsJsonAsync("api/auth/login", request, token);
        if (!result.IsSuccessStatusCode)
        {
            return false;
        }

        string receivedJwt = await result.Content.ReadAsStringAsync(token);

        await _localStorageService.SetItemAsync(JwtAuthStateProvider.AuthTokenKey, receivedJwt);
        _authStateProvider.NotifyUserAuthentication(receivedJwt);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", receivedJwt);

        return true;
    }

    public async Task Logout()
    {
        await _localStorageService.RemoveItemAsync(JwtAuthStateProvider.AuthTokenKey);
        _authStateProvider.NotifyUserLogout();
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    // --- Master Lists ---

    public async Task<MasterListRow[]?> CreateMasterList(CreateMasterListRequest createRequest, CancellationToken token)
    {
        var response = await _httpClient.PostAsJsonAsync("api/masterlist/create", createRequest, token);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return null;
        }

        MasterListRow[] list = (await response.Content.ReadFromJsonAsync<MasterListRow[]>(cancellationToken: token))!;
        return list;
    }

    public async Task<MasterListRow[]?> GetMasterList(Guid listId, CancellationToken token)
    {
        var response = await _httpClient.GetAsync($"api/masterlist/{listId}", token);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return null;
        }

        MasterListRow[] list = (await response.Content.ReadFromJsonAsync<MasterListRow[]>(cancellationToken: token))!;
        return list;
    }

    public async Task<SavedMasterList[]?> GetMasterLists(CancellationToken token)
    {
        var response = await _httpClient.GetAsync("api/masterlist", token);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return null;
        }

        var savedLists = (await response.Content.ReadFromJsonAsync<SavedMasterList[]>())!;
        return savedLists;
    }

    public async Task<MasterListRow[]?> EditMasterList(Guid listId, EditMasterListRequest editRequest, CancellationToken token)
    {
        string serializedEditRequest = JsonSerializer.Serialize(editRequest);
        StringContent editContent = new StringContent(serializedEditRequest, Encoding.UTF8, "application/json");
        var response = await _httpClient.PatchAsync($"/api/masterlist/{listId}/edit", editContent, token);
        if (!response.IsSuccessStatusCode)
        {
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return null;
        }

        MasterListRow[] updatedList = (await response.Content.ReadFromJsonAsync<MasterListRow[]>(cancellationToken: token))!;
        return updatedList;
    }

    public async Task<bool> DeleteMasterList(Guid listId, CancellationToken token)
    {
        var response = await _httpClient.DeleteAsync($"/api/masterlist/{listId}/delete", token);
        if (!response.IsSuccessStatusCode)
        {
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return false;
        }

        return true;
    }

    // Tests
    public async Task<TestRow[]?> CreateTest(CreateTestRequest createTestRequest, CancellationToken token)
    {
        var response = await _httpClient.PostAsJsonAsync<CreateTestRequest>($"/api/tests/create", createTestRequest, token);
        if (!response.IsSuccessStatusCode)
        {
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return null;
        }

        TestRow[] returnedRows = (await response.Content.ReadFromJsonAsync<TestRow[]>(cancellationToken: token))!;
        return returnedRows;
    }

    public async Task<TestOverviewResponse?> GetTestOverview(string? continuationToken, CancellationToken token)
    {
        string queryFragment = continuationToken != null ? $"?con={continuationToken}" : "";
        var response = await _httpClient.GetAsync($"/api/tests/overview{queryFragment}", token);
        if (!response.IsSuccessStatusCode)
        {
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return null;
        }

        TestOverviewResponse rowsAndToken = (
            await response.Content.ReadFromJsonAsync<TestOverviewResponse>(cancellationToken: token)
        )!;
        return rowsAndToken;
    }

    public async Task<bool> EditTestOverview(EditTestOverviewRequest request, CancellationToken token)
    {
        string serializedRequest = JsonSerializer.Serialize(request);
        StringContent editContent = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
        var response = await _httpClient.PatchAsync($"/api/tests/overview/{request.TestId.ToString()}/edit", editContent, token);
        if (!response.IsSuccessStatusCode)
        {
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return false;
        }

        return true;
    }

    public async Task<TestWithResults?> GetTestDetails(Guid testId, CancellationToken token)
    {
        var response = await _httpClient.GetAsync($"/api/tests/{testId}", token);
        if (!response.IsSuccessStatusCode)
        {
            if (response?.StatusCode == HttpStatusCode.Unauthorized)
            {
                RedirectToLogin();
            }

            return null;
        }

        TestWithResults testDetails = (await response.Content.ReadFromJsonAsync<TestWithResults>(cancellationToken: token))!;
        return testDetails;
    }

    private void RedirectToLogin()
    {
        _navManager.NavigateTo($"/login?originalUrl={Uri.EscapeDataString(_navManager.Uri)}");
    }
}

public interface INetworkService
{
    // Login/out
    Task<bool> Login(LoginRequest request, CancellationToken token);
    Task Logout();

    // Master lists
    Task<MasterListRow[]?> CreateMasterList(CreateMasterListRequest createRequest, CancellationToken token);
    Task<MasterListRow[]?> GetMasterList(Guid listId, CancellationToken token);
    Task<SavedMasterList[]?> GetMasterLists(CancellationToken token);
    Task<MasterListRow[]?> EditMasterList(Guid listId, EditMasterListRequest editRequest, CancellationToken token);
    Task<bool> DeleteMasterList(Guid listId, CancellationToken token);

    // Tests
    Task<TestRow[]?> CreateTest(CreateTestRequest createTestRequest, CancellationToken token);
    Task<TestOverviewResponse?> GetTestOverview(string? continuationToken, CancellationToken token);
    Task<bool> EditTestOverview(EditTestOverviewRequest request, CancellationToken token);
    Task<TestWithResults?> GetTestDetails(Guid testId, CancellationToken token);
}

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VerbIt.Client.Authentication;
using VerbIt.DataModels;

namespace VerbIt.Client.Services
{
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
            var response = await _httpClient.GetAsync($"api/masterlist/{listId}");
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
    }
}

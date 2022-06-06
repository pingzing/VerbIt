using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VerbIt.Client.Authentication;
using VerbIt.Client.Models;
using VerbIt.DataModels;

namespace VerbIt.Client.Services
{
    public class NetworkService : INetworkService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly HttpClient _httpClient;
        private readonly JwtAuthStateProvider _authStateProvider;
        private readonly ILocalStorageService _localStorageService;

        public NetworkService(
            HttpClient httpClient,
            JwtAuthStateProvider authStateProvider,
            ILocalStorageService localStorageService
        )
        {
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _localStorageService = localStorageService;
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

        public async Task<Result<MasterListRow[], NetworkError>> CreateMasterList(
            CreateMasterListRequest createRequest,
            CancellationToken token
        )
        {
            var response = await _httpClient.PostAsJsonAsync("api/masterlist/create", createRequest, token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Result<MasterListRow[], NetworkError>.Error(NetworkError.Unauthorized);
                }
            }

            MasterListRow[] list = (await response.Content.ReadFromJsonAsync<MasterListRow[]>(cancellationToken: token))!;
            return Result<MasterListRow[], NetworkError>.Ok(list);
        }

        public async Task<Result<SavedMasterList[], NetworkError>> GetMasterLists(CancellationToken token)
        {
            var response = await _httpClient.GetAsync("api/masterlist", token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return Result<SavedMasterList[], NetworkError>.Error(NetworkError.Unauthorized);
                }

                return Result<SavedMasterList[], NetworkError>.Error(NetworkError.InternalServerError);
            }

            var savedLists = (await response.Content.ReadFromJsonAsync<SavedMasterList[]>())!;
            return Result<SavedMasterList[], NetworkError>.Ok(savedLists);
        }
    }

    public interface INetworkService
    {
        // Login/out
        Task<bool> Login(LoginRequest request, CancellationToken token);
        Task Logout();

        // Master lists
        Task<Result<MasterListRow[], NetworkError>> CreateMasterList(
            CreateMasterListRequest createRequest,
            CancellationToken token
        );

        Task<Result<SavedMasterList[], NetworkError>> GetMasterLists(CancellationToken token);
    }
}

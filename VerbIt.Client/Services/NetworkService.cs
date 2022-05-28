using System.Net.Http.Json;
using VerbIt.DataModels;

namespace VerbIt.Client.Services
{
    public class NetworkService : INetworkService
    {
        private readonly HttpClient _httpClient;

        public NetworkService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> Login(LoginRequest request, CancellationToken token)
        {
            HttpResponseMessage result = await _httpClient.PostAsJsonAsync(
                "api/login",
                request,
                token
            );
            if (!result.IsSuccessStatusCode)
            {
                return "boo";
            }

            return "yay";
        }
    }

    public interface INetworkService
    {
        Task<string> Login(LoginRequest request, CancellationToken token);
    }
}

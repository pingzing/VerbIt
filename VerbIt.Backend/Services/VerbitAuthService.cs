using VerbIt.Backend.Repositories;
using VerbIt.DataModels;

namespace VerbIt.Backend.Services;

public class VerbitAuthService : IVerbitAuthService
{
    private readonly IVerbitRepository _verbitRepository;

    public VerbitAuthService(IVerbitRepository verbitRepository)
    {
        _verbitRepository = verbitRepository;
    }

    public async Task<AuthenticatedUser?> Login(string username, string password, CancellationToken token)
    {
        return await _verbitRepository.AuthenticateUser(username, password, token);
    }
}

public interface IVerbitAuthService
{
    Task<AuthenticatedUser?> Login(string username, string password, CancellationToken token);
}

using VerbIt.DataModels;

namespace VerbIt.Backend.Repositories;

public class VerbitRepository : IVerbitRepository
{
    public async Task<AuthenticatedUser?> AuthenticateUser(string username, string password)
    {
        // TODO: Actually hit a database and validate this info
        await Task.Delay(1);
        return new AuthenticatedUser(Guid.NewGuid(), "User", "Admin");
    }
}

public interface IVerbitRepository
{
    public Task<AuthenticatedUser?> AuthenticateUser(string username, string password);
}

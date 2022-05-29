using Azure.Data.Tables;
using VerbIt.DataModels;

namespace VerbIt.Backend.Repositories;

public class VerbitRepository : IVerbitRepository
{
    private readonly TableServiceClient _tableServiceClient;

    public VerbitRepository(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }

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

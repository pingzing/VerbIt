using Azure;
using Azure.Data.Tables;
using Isopoh.Cryptography.Argon2;
using VerbIt.Backend.Entities;
using VerbIt.DataModels;

namespace VerbIt.Backend.Repositories;

public class VerbitRepository : IVerbitRepository
{
    private const string MasterListTableName = "MasterList";
    private const string AdminUserTableName = "AdminUsers";

    private readonly ILogger<VerbitRepository> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public VerbitRepository(TableServiceClient tableServiceClient, ILogger<VerbitRepository> logger)
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;

        // Create all the well-known tables that will need to exist.
        Task.WaitAll(
            new[]
            {
                Task.Run(() => _tableServiceClient.CreateTableIfNotExists(MasterListTableName)),
                Task.Run(() => _tableServiceClient.CreateTableIfNotExists(AdminUserTableName)),
            }
        );
    }

    // --- Auth ---

    public async Task<AuthenticatedUser?> AuthenticateUser(string username, string password, CancellationToken token)
    {
        // TODO: Actually hit a database and validate this info
        await Task.Delay(1);
        return new AuthenticatedUser("User", VerbitRoles.Admin);
    }

    // --- Master Lists ---

    public async Task CreateMasterList(MasterList newList, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        MasterListEntity newListEntity = MasterListEntity.FromDTO(newList);
        try
        {
            await tableClient.AddEntityAsync(newListEntity, token);
        }
        catch (Exception ex) when (ex is RequestFailedException rfEx)
        {
            _logger.LogError($"Failed to insert Master List with name {newList.Number} into MasterList table.");
            throw new StatusCodeException(
                StatusCodes.Status500InternalServerError,
                $"Table Storage failed to insert. HTTP code: {rfEx.Status}, message: {rfEx.Message}",
                rfEx
            );
        }
    }

    // --- Admin Users ---
    public async Task<AuthenticatedUser> CreateAdminUser(string username, string password, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(AdminUserTableName);
        AdminUserEntity userEntity = new AdminUserEntity
        {
            PartitionKey = AdminUserEntity.DefaultPartitionKey,
            RowKey = username,
            HashedPassword = Argon2.Hash(password),
        };
        try
        {
            Response response = await tableClient.AddEntityAsync(userEntity, token);
            AdminUserEntity addedEntity = response.Content.ToObjectFromJson<AdminUserEntity>();

            // Probably don't really need this.
            if (!Argon2.Verify(addedEntity.HashedPassword, password))
            {
                throw new StatusCodeException(
                    StatusCodes.Status500InternalServerError,
                    "Stored password hash did not match user-sent password"
                );
            }

            return new AuthenticatedUser(username, VerbitRoles.Admin);
        }
        catch (Exception ex) when (ex is RequestFailedException rfEx)
        {
            _logger.LogError(
                "Failed to insert Admin User with name {Username} into AdminUser table. Details: {ExceptionMessage}",
                userEntity.Name,
                rfEx.Message
            );

            if (rfEx.Status == StatusCodes.Status409Conflict)
            {
                throw new StatusCodeException(StatusCodes.Status400BadRequest);
            }
            else
            {
                throw new StatusCodeException(StatusCodes.Status500InternalServerError);
            }
        }
    }

    public async Task<AuthenticatedUser> GetAdminUser(string username, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(AdminUserTableName);

        try
        {
            Response<AdminUserEntity> result = await tableClient.GetEntityAsync<AdminUserEntity>(
                AdminUserEntity.DefaultPartitionKey,
                username,
                cancellationToken: token
            );

            return new AuthenticatedUser(result.Value.Name, VerbitRoles.Admin);
        }
        catch (Exception ex) when (ex is RequestFailedException rfEx)
        {
            _logger.LogError(
                "Failed to get admin user with name {Username}. Details: {ExceptionMessage}",
                username,
                rfEx.Message
            );

            if (rfEx.Status == StatusCodes.Status404NotFound)
            {
                throw new StatusCodeException(StatusCodes.Status404NotFound);
            }
            else
            {
                throw new StatusCodeException(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

public interface IVerbitRepository
{
    // Auth
    Task<AuthenticatedUser?> AuthenticateUser(string username, string password, CancellationToken token);

    // Master lists
    Task CreateMasterList(MasterList newList, CancellationToken token);

    // Admin users
    Task<AuthenticatedUser> CreateAdminUser(string username, string password, CancellationToken token);
    Task<AuthenticatedUser> GetAdminUser(string username, CancellationToken token);
}

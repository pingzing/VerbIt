using Azure;
using Azure.Data.Tables;
using Isopoh.Cryptography.Argon2;
using System.Globalization;
using System.Text.Json;
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
        TableClient tableClient = _tableServiceClient.GetTableClient(AdminUserTableName);

        try
        {
            Response<AdminUserEntity> response = await tableClient.GetEntityAsync<AdminUserEntity>(
                AdminUserEntity.DefaultPartitionKey,
                username,
                cancellationToken: token
            );

            if (!Argon2.Verify(response.Value.HashedPassword, password))
            {
                throw new StatusCodeException(StatusCodes.Status404NotFound, "No user with that username or password found");
            }

            return new AuthenticatedUser(response.Value.Name, VerbitRoles.Admin);
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status == StatusCodes.Status404NotFound)
            {
                throw new StatusCodeException(StatusCodes.Status404NotFound, "No user with that username or password found", ex);
            }

            throw new StatusCodeException(StatusCodes.Status500InternalServerError, "Internal server error", ex);
        }
    }

    // --- Master Lists ---

    public async Task<MasterListRow[]> CreateMasterList(MasterListRow[] newList, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            IEnumerable<TableTransactionAction> addEntitiesBatch = newList.Select(
                x => new TableTransactionAction(TableTransactionActionType.Add, MasterListRowEntity.FromDTO(x))
            );

            Response<IReadOnlyList<Response>> response = await tableClient.SubmitTransactionAsync(addEntitiesBatch, token);

            // Get updated entities, as there's no way to set Prefer: return-content on a batch insert yet
            List<MasterListRow> newRows = new();
            await foreach (
                MasterListRowEntity newEntity in tableClient.QueryAsync<MasterListRowEntity>(
                    x => x.PartitionKey == newList[0].Name,
                    cancellationToken: token
                )
            )
            {
                newRows.Add(newEntity.AsDTO());
            }

            return newRows.ToArray();
        }
        catch (Exception ex)
        {
            if (ex is TableTransactionFailedException ttfEx)
            {
                string errorFragment = "";
                if (ttfEx.FailedTransactionActionIndex != null)
                {
                    errorFragment =
                        $" Object at index: {JsonSerializer.Serialize(newList[ttfEx.FailedTransactionActionIndex.Value])}";
                }
                _logger.LogError(
                    $"Transaciton failed when creating new table at index: {ttfEx.FailedTransactionActionIndex}.{errorFragment}"
                );

                // TODO: Investigate what kind of errors we catch here.
                throw new StatusCodeException(StatusCodes.Status400BadRequest);
            }

            throw new StatusCodeException(
                StatusCodes.Status500InternalServerError,
                $"Failed to insert Master List with name {newList[0].Name} into MasterList table.",
                ex
            );
        }
    }

    public async Task EditMasterList(EditMasterListRequest editedList, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            IEnumerable<TableTransactionAction> batch = editedList.ChangedRows.Select(
                x =>
                    x switch
                    {
                        MasterListRowDeleteRequest req
                            => new TableTransactionAction(
                                TableTransactionActionType.Delete,
                                DeleteMasterListRowEntity.FromDTO(req, editedList.ListName)
                            ),
                        MasterListRowEditRequest req
                            => new TableTransactionAction(
                                TableTransactionActionType.UpdateMerge,
                                EditMasterListRowEntity.FromDTO(req, editedList.ListName)
                            ),
                        _ => throw new StatusCodeException(StatusCodes.Status400BadRequest)
                    }
            );

            await tableClient.SubmitTransactionAsync(batch, token);
        }
        catch (Exception ex)
        {
            if (ex is TableTransactionFailedException ttfEx)
            {
                string errorFragment = "";
                if (ttfEx.FailedTransactionActionIndex != null)
                {
                    errorFragment =
                        $" Object at index: {JsonSerializer.Serialize(editedList.ChangedRows[ttfEx.FailedTransactionActionIndex.Value])}";
                }
                _logger.LogError(
                    $"Transaction failed when creating new table at index: {ttfEx.FailedTransactionActionIndex}.{errorFragment}"
                );

                // TODO: Investigate what kind of errors we catch here.
                throw new StatusCodeException(StatusCodes.Status400BadRequest);
            }

            throw new StatusCodeException(StatusCodes.Status500InternalServerError, $"Failed to edit Master List", ex);
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
        catch (RequestFailedException ex)
        {
            if (ex.Status == StatusCodes.Status409Conflict)
            {
                throw new StatusCodeException(StatusCodes.Status400BadRequest, "Invalid request", ex);
            }

            throw new StatusCodeException(StatusCodes.Status500InternalServerError, "Internal server error", ex);
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
        catch (RequestFailedException ex)
        {
            if (ex.Status == StatusCodes.Status404NotFound)
            {
                throw new StatusCodeException(StatusCodes.Status404NotFound, "No user with that name found", ex);
            }
            else
            {
                throw new StatusCodeException(StatusCodes.Status500InternalServerError, "Internal server error", ex);
            }
        }
    }
}

public interface IVerbitRepository
{
    // Auth
    Task<AuthenticatedUser?> AuthenticateUser(string username, string password, CancellationToken token);

    // Master lists
    Task<MasterListRow[]> CreateMasterList(MasterListRow[] newList, CancellationToken token);
    Task EditMasterList(EditMasterListRequest editedList, CancellationToken token);

    // Admin users
    Task<AuthenticatedUser> CreateAdminUser(string username, string password, CancellationToken token);
    Task<AuthenticatedUser> GetAdminUser(string username, CancellationToken token);
}

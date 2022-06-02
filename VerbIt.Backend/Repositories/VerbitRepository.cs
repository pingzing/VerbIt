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

    public async Task<MasterListRow[]> GetMasterList(Guid listId, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            List<MasterListRowEntity> masterList = new();
            await foreach (
                MasterListRowEntity row in tableClient.QueryAsync<MasterListRowEntity>(
                    x => x.PartitionKey == listId.ToString(),
                    cancellationToken: token
                )
            )
            {
                masterList.Add(row);
            }

            return masterList.Select(x => x.AsDTO()).ToArray();
        }
        catch (RequestFailedException ex)
        {
            // TODO: Investigate failures we can get here
            _logger.LogError(ex, "Failed to get List at {listId}", listId);
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, $"Failed to get List ID {listId}", ex);
        }
    }

    public async Task<MasterListRow[]> CreateMasterList(CreateMasterListRequest createRequest, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            Guid listId = Guid.NewGuid();
            int rowNum = 1;
            DateTimeOffset listCreationTimestamp = DateTimeOffset.UtcNow;
            IEnumerable<TableTransactionAction> addEntitiesBatch = createRequest.Rows.Select(
                x =>
                    new TableTransactionAction(
                        TableTransactionActionType.Add,
                        MasterListRowEntity.FromCreateRequest(
                            listId,
                            createRequest.Name,
                            rowNum++,
                            x.Words,
                            listCreationTimestamp
                        )
                    )
            );

            Response<IReadOnlyList<Response>> response = await tableClient.SubmitTransactionAsync(addEntitiesBatch, token);

            // Get updated entities, as there's no way to set Prefer: return-content on a batch insert yet
            List<MasterListRow> newRows = new();
            await foreach (
                MasterListRowEntity newEntity in tableClient.QueryAsync<MasterListRowEntity>(
                    x => x.PartitionKey == listId.ToString(),
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
                        $" Object at index: {JsonSerializer.Serialize(createRequest.Rows[ttfEx.FailedTransactionActionIndex.Value])}";
                }
                _logger.LogError(
                    $"Transaciton failed when creating new table at index: {ttfEx.FailedTransactionActionIndex}.{errorFragment}"
                );

                // TODO: Investigate what kind of errors we catch here.
                throw new StatusCodeException(StatusCodes.Status400BadRequest);
            }

            throw new StatusCodeException(
                StatusCodes.Status500InternalServerError,
                $"Failed to insert Master List with name {createRequest.Name} into MasterList table.",
                ex
            );
        }
    }

    public async Task<MasterListRow[]> DeleteMasterListRows(DeleteMasterListRowsRequest deleteRowRequest, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public async Task EditMasterList(EditMasterListRequest editedList, Guid[]? rowIdsToUpdateName, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            List<TableTransactionAction> actions = new List<TableTransactionAction>();
            actions.AddRange(
                editedList.Rows.Select(x =>
                {
                    var updateEntity = new TableEntity(editedList.ListId.ToString(), x.RowId.ToString());
                    if (editedList.ListName != null)
                    {
                        updateEntity[nameof(MasterListRowEntity.ListName)] = editedList.ListName;
                    }
                    if (x.RowNum != null)
                    {
                        updateEntity[nameof(MasterListRowEntity.RowNum)] = x.RowNum;
                    }
                    if (x.Words != null)
                    {
                        updateEntity[nameof(MasterListRowEntity.WordsJson)] = JsonSerializer.Serialize(x.Words);
                    }

                    return new TableTransactionAction(TableTransactionActionType.UpdateMerge, updateEntity);
                })
            );

            if (rowIdsToUpdateName != null)
            {
                actions.AddRange(
                    rowIdsToUpdateName.Select(
                        rowId =>
                            new TableTransactionAction(
                                TableTransactionActionType.UpdateMerge,
                                new TableEntity(editedList.ListId.ToString(), rowId.ToString())
                                {
                                    { nameof(MasterListRowEntity.ListName), editedList.ListName },
                                }
                            )
                    )
                );
            }

            Response<IReadOnlyList<Response>> response = await tableClient.SubmitTransactionAsync(actions, token);
        }
        catch (RequestFailedException ex)
        {
            // TODO: invetigate how we fail here.

            throw new StatusCodeException(
                StatusCodes.Status500InternalServerError,
                $"Failed to edit Master List with ID {editedList.ListId} into MasterList table.",
                ex
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
    Task<MasterListRow[]> CreateMasterList(CreateMasterListRequest createRequest, CancellationToken token);
    Task<MasterListRow[]> GetMasterList(Guid listId, CancellationToken token);
    Task EditMasterList(EditMasterListRequest editedList, Guid[]? rowIdsToUpdateName, CancellationToken token);
    Task<MasterListRow[]> DeleteMasterListRows(DeleteMasterListRowsRequest deleteRowsRequest, CancellationToken token);

    // Admin users
    Task<AuthenticatedUser> CreateAdminUser(string username, string password, CancellationToken token);
    Task<AuthenticatedUser> GetAdminUser(string username, CancellationToken token);
}

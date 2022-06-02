using Azure;
using Azure.Data.Tables;
using Isopoh.Cryptography.Argon2;
using System.Text.Json;
using VerbIt.Backend.Entities;
using VerbIt.DataModels;

namespace VerbIt.Backend.Repositories;

public class VerbitRepository : IVerbitRepository
{
    private const string MasterListTableName = "MasterList";
    private const string SavedMasterListTableName = "SavedMasterLists";
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
                Task.Run(() => _tableServiceClient.CreateTableIfNotExists(SavedMasterListTableName)),
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

    public async Task<SavedMasterList[]> GetSavedMasterLists(CancellationToken token)
    {
        TableClient client = _tableServiceClient.GetTableClient(SavedMasterListTableName);
        try
        {
            var getSavedMasterListsQuery = client.QueryAsync<SavedMasterListEntity>(
                x => x.PartitionKey == SavedMasterListEntity.DefaultPartitionKey,
                cancellationToken: token
            );

            List<SavedMasterListEntity> savedLists = new();
            await foreach (SavedMasterListEntity savedMasterList in getSavedMasterListsQuery)
            {
                savedLists.Add(savedMasterList);
            }

            return savedLists.Select(x => x.AsDTO()).ToArray();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, $"Failed to get saved master lists.");
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, "Failed to get saved master lists.");
        }
    }

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
                createRowRequest =>
                    new TableTransactionAction(
                        TableTransactionActionType.Add,
                        MasterListRowEntity.FromCreateRequest(
                            listId,
                            createRequest.Name,
                            rowNum++,
                            createRowRequest.Words,
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

            // Add a new entry to the "every list ID" tracker
            await _tableServiceClient
                .GetTableClient(SavedMasterListTableName)
                .AddEntityAsync(
                    new SavedMasterListEntity
                    {
                        PartitionKey = SavedMasterListEntity.DefaultPartitionKey,
                        RowKey = listId.ToString(),
                        ListName = createRequest.Name,
                        ListCreationTimestamp = listCreationTimestamp,
                    }
                );

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

    // This both deletes rows, and performs fixups on all rows in the list to fix their rownums.
    public async Task<MasterListRow[]> DeleteMasterListRows(
        Guid listId,
        MasterListRow[] listRows,
        HashSet<Guid> rowsToDelete,
        CancellationToken token
    )
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            IEnumerable<TableTransactionAction> transactionActions = listRows.Select(x =>
            {
                if (rowsToDelete.Contains(x.RowId))
                {
                    return new TableTransactionAction(
                        TableTransactionActionType.Delete,
                        new TableEntity(listId.ToString(), x.RowId.ToString())
                    );
                }
                else
                {
                    return new TableTransactionAction(
                        TableTransactionActionType.UpdateMerge,
                        new TableEntity(listId.ToString(), x.RowId.ToString())
                        {
                            { nameof(MasterListRowEntity.RowNum), x.RowNum }
                        }
                    );
                }
            });

            Response<IReadOnlyList<Response>> response = await tableClient.SubmitTransactionAsync(transactionActions, token);

            // Get updated list, until we figure out a way to make transactions return resutls
            return await GetMasterList(listId, token);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete list rows. Investigate this further.");
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, "Failed to delete the given row IDs.");
        }
    }

    public async Task DeleteMasterList(Guid listId, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            MasterListRow[] existingList = await GetMasterList(listId, token);
            var actions = existingList.Select(
                x =>
                    new TableTransactionAction(
                        TableTransactionActionType.Delete,
                        new TableEntity(listId.ToString(), x.RowId.ToString())
                    )
            );

            await tableClient.SubmitTransactionAsync(actions, token);

            await _tableServiceClient
                .GetTableClient(SavedMasterListTableName)
                .DeleteEntityAsync(SavedMasterListEntity.DefaultPartitionKey, listId.ToString(), cancellationToken: token);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete list with ID of {listId}", listId.ToString());
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, $"Failed to delete Master List ID {listId}");
        }
    }

    public async Task EditMasterList(
        Guid listId,
        EditMasterListRequest editedList,
        Guid[]? rowIdsToUpdateName,
        CancellationToken token
    )
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            List<TableTransactionAction> actions = new List<TableTransactionAction>();
            if (editedList.Rows != null)
            {
                actions.AddRange(
                    editedList.Rows.Select(editRowRequest =>
                    {
                        var updateEntity = new TableEntity(listId.ToString(), editRowRequest.RowId.ToString());
                        if (editedList.ListName != null)
                        {
                            updateEntity[nameof(MasterListRowEntity.ListName)] = editedList.ListName;
                        }
                        if (editRowRequest.RowNum != null)
                        {
                            updateEntity[nameof(MasterListRowEntity.RowNum)] = editRowRequest.RowNum;
                        }
                        if (editRowRequest.Words != null)
                        {
                            updateEntity[nameof(MasterListRowEntity.WordsJson)] = JsonSerializer.Serialize(editRowRequest.Words);
                        }

                        return new TableTransactionAction(TableTransactionActionType.UpdateMerge, updateEntity);
                    })
                );
            }

            if (rowIdsToUpdateName != null)
            {
                actions.AddRange(
                    rowIdsToUpdateName.Select(
                        rowId =>
                            new TableTransactionAction(
                                TableTransactionActionType.UpdateMerge,
                                new TableEntity(listId.ToString(), rowId.ToString())
                                {
                                    { nameof(MasterListRowEntity.ListName), editedList.ListName },
                                }
                            )
                    )
                );
            }

            Response<IReadOnlyList<Response>> response = await tableClient.SubmitTransactionAsync(actions, token);

            if (editedList.ListName != null)
            {
                await UpdatedSavedMasterListName(listId, editedList.ListName, token);
            }
        }
        catch (RequestFailedException ex)
        {
            // TODO: invetigate how we fail here.

            throw new StatusCodeException(
                StatusCodes.Status500InternalServerError,
                $"Failed to edit Master List with ID {listId} into MasterList table.",
                ex
            );
        }
    }

    public async Task<MasterListRow[]> AddMasterListRows(Guid listId, AddMasterListRowsRequest request, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient(MasterListTableName);
        try
        {
            // Get the last existing row
            List<MasterListRowEntity> existingList = new();
            tableClient.QueryAsync<MasterListRowEntity>(
                x => x.PartitionKey == listId.ToString(),
                select: new[] { nameof(MasterListRowEntity.RowNum) }
            );
            MasterListRowEntity lastRow = existingList.MaxBy(x => x.RowNum)!;

            int rowNum = lastRow.RowNum + 1;
            IEnumerable<TableTransactionAction> actions = request.Rows.Select(
                createRowRequest =>
                    new TableTransactionAction(
                        TableTransactionActionType.UpsertReplace,
                        MasterListRowEntity.FromCreateRequest(
                            listId,
                            lastRow.ListName,
                            rowNum++,
                            createRowRequest.Words,
                            lastRow.ListCreationTimestamp
                        )
                    )
            );

            var response = await tableClient.SubmitTransactionAsync(actions, token);

            // Get updated list, until we figure out a way to make transactions return resutls
            return await GetMasterList(listId, token);
        }
        catch (RequestFailedException ex)
        {
            // TODO: invetigate how we fail here.

            throw new StatusCodeException(
                StatusCodes.Status500InternalServerError,
                $"Failed to add Master List rows to list with ID {listId}.",
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

    // Tucked away into its own little method, because the SDK has a bug where it throws
    // if Perfer: return-content is set and we do a merge-update.
    private async Task UpdatedSavedMasterListName(Guid listId, string newName, CancellationToken token)
    {
        var savedListclient = _tableServiceClient.GetTableClient(SavedMasterListTableName);
        var entity = new TableEntity(SavedMasterListEntity.DefaultPartitionKey, listId.ToString())
        {
            { nameof(SavedMasterListEntity.ListName), newName }
        };

        try
        {
            await savedListclient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Merge, cancellationToken: token);
        }
        catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status200OK) { }
    }
}

public interface IVerbitRepository
{
    // Auth
    Task<AuthenticatedUser?> AuthenticateUser(string username, string password, CancellationToken token);

    // Master lists

    /// <summary>
    /// Retrieves the IDs, names, and creation times of <em>all</em> master lists.
    /// </summary>
    Task<SavedMasterList[]> GetSavedMasterLists(CancellationToken token);
    Task<MasterListRow[]> CreateMasterList(CreateMasterListRequest createRequest, CancellationToken token);
    Task<MasterListRow[]> GetMasterList(Guid listId, CancellationToken token);
    Task EditMasterList(Guid listId, EditMasterListRequest editedList, Guid[]? rowIdsToUpdateName, CancellationToken token);

    /// <summary>
    /// Deletes the rows with the IDs in <paramref name="rowsToDelete"/>, and simultaneously updates
    /// Row Nums based on those passed in via <paramref name="listRows"/>.
    /// </summary>
    /// <param name="listId">The ID of the list to delete rows from.</param>
    /// <param name="listRows">All the rows whose RowNums need to be updated as a result of deletions.</param>
    /// <param name="rowsToDelete">IDs of the rows to delete.</param>
    /// <returns>The Master List rows, with deleted rows removed, and Row Nums updated.</returns>
    Task<MasterListRow[]> DeleteMasterListRows(
        Guid listId,
        MasterListRow[] listRows,
        HashSet<Guid> rowsToDelete,
        CancellationToken token
    );

    Task DeleteMasterList(Guid listId, CancellationToken token);

    // Admin users
    Task<AuthenticatedUser> CreateAdminUser(string username, string password, CancellationToken token);
    Task<AuthenticatedUser> GetAdminUser(string username, CancellationToken token);
    Task<MasterListRow[]> AddMasterListRows(Guid listId, AddMasterListRowsRequest request, CancellationToken token);
}

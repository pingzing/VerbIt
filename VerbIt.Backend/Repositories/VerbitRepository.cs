using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using VerbIt.Backend.Entities;
using VerbIt.Backend.Models;
using VerbIt.DataModels;

namespace VerbIt.Backend.Repositories;

public class VerbitRepository : IVerbitRepository
{
    private const string AdminUserTableName = "AdminUsers";

    private const string MasterListTableName = "MasterList";
    private const string SavedMasterListTableName = "SavedMasterLists";

    public const string TestsOverviewTableName = "TestsOverview";
    public const string TestsTableName = "Tests";
    public const string AvailableTestsTableName = "AvailableTests";
    public const string TestResultsTableName = "TestResults";
    public const string TestResultsOverviewTableName = "TestResultsOverview";

    // Used to make it easy to create all the tables in the constructor.
    // New tables must be manually added here.
    public static readonly string[] _tableNames = new[]
    {
        // Users
        AdminUserTableName,
        //Master Lists
        MasterListTableName,
        SavedMasterListTableName,
        //Tests
        TestsOverviewTableName,
        TestsTableName,
        AvailableTestsTableName,
        TestResultsTableName,
        TestResultsOverviewTableName,
    };

    private readonly string _tablePrefix = "";
    private readonly ILogger<VerbitRepository> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public VerbitRepository(
        TableServiceClient tableServiceClient,
        ILogger<VerbitRepository> logger,
        IOptions<TableStorageSettings> settings
    )
    {
        _tableServiceClient = tableServiceClient;
        _logger = logger;
        _tablePrefix = settings.Value.TablePrefix;

        // Create all the well-known tables that will need to exist.
        Task<Response<TableItem>>[] createTableTasks = _tableNames
            .Select(tableName => _tableServiceClient.CreateTableIfNotExistsAsync($"{_tablePrefix}{tableName}"))
            .ToArray();

        Task.WaitAll(createTableTasks);
    }

    // --- Auth ---

    public async Task<AuthenticatedUser?> AuthenticateUser(string username, string password, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{AdminUserTableName}");

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
        TableClient client = _tableServiceClient.GetTableClient($"{_tablePrefix}{SavedMasterListTableName}");
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
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{MasterListTableName}");
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
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{MasterListTableName}");
        try
        {
            Guid listId = Guid.NewGuid();
            int rowNum = 1;
            DateTimeOffset listCreationTimestamp = DateTimeOffset.UtcNow;
            List<MasterListRowEntity> rowsToCreate = createRequest.Rows
                .Select(
                    createRowRequest =>
                        MasterListRowEntity.FromCreateRequest(
                            listId,
                            createRequest.Name,
                            rowNum++,
                            createRowRequest.Words,
                            listCreationTimestamp
                        )
                )
                .ToList();

            var addEntitiesBatches = rowsToCreate
                .Select(row => new TableTransactionAction(TableTransactionActionType.Add, row))
                .Chunk(100)
                .Select(x => tableClient.SubmitTransactionAsync(x, token));

            // Await all batch chunks simultaneously
            Response<IReadOnlyList<Response>>[] responses = await Task.WhenAll(addEntitiesBatches);

            // Add a new entry to the "every list ID" tracker
            await _tableServiceClient
                .GetTableClient($"{_tablePrefix}{SavedMasterListTableName}")
                .AddEntityAsync(
                    new SavedMasterListEntity
                    {
                        PartitionKey = SavedMasterListEntity.DefaultPartitionKey,
                        RowKey = listId.ToString(),
                        ListName = createRequest.Name,
                        ListCreationTimestamp = listCreationTimestamp,
                        TotalRows = rowsToCreate.Count,
                    },
                    token
                );

            return rowsToCreate.Select(x => x.AsDTO()).ToArray();
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
        IEnumerable<MasterListRow> listRows,
        HashSet<Guid> rowsToDelete,
        CancellationToken token
    )
    {
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{MasterListTableName}");
        try
        {
            var transactionActions = listRows
                .Select(x =>
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
                })
                .Chunk(100)
                .Select(x => tableClient.SubmitTransactionAsync(x, token));

            Response<IReadOnlyList<Response>>[] responses = await Task.WhenAll(transactionActions);

            MasterListRow[] updatedList = await GetMasterList(listId, token);
            await UpdateSavedMasterList(listId, null, updatedList.Length, token);

            return updatedList;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete list rows. Investigate this further.");
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, "Failed to delete the given row IDs.");
        }
    }

    public async Task DeleteMasterList(Guid listId, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{MasterListTableName}");
        try
        {
            MasterListRow[] existingList = await GetMasterList(listId, token);

            var actions = existingList
                .Select(
                    x =>
                        new TableTransactionAction(
                            TableTransactionActionType.Delete,
                            new TableEntity(listId.ToString(), x.RowId.ToString())
                        )
                )
                .Chunk(100)
                .Select(x => tableClient.SubmitTransactionAsync(x, token));

            Response<IReadOnlyList<Response>>[] responses = await Task.WhenAll(actions);

            await _tableServiceClient
                .GetTableClient($"{_tablePrefix}{SavedMasterListTableName}")
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
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{MasterListTableName}");
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

            var actionChunks = actions.Chunk(100).Select(x => tableClient.SubmitTransactionAsync(x, token));

            Response<IReadOnlyList<Response>>[] responses = await Task.WhenAll(actionChunks);

            if (editedList.ListName != null)
            {
                await UpdateSavedMasterList(listId, editedList.ListName, null, token);
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
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{MasterListTableName}");
        try
        {
            // Get the last existing row
            List<MasterListRowEntity> existingList = new();

            await foreach (
                MasterListRowEntity existingListRow in tableClient.QueryAsync<MasterListRowEntity>(
                    x => x.PartitionKey == listId.ToString(),
                    select: new[] { nameof(MasterListRowEntity.RowNum) },
                    cancellationToken: token
                )
            )
            {
                existingList.Add(existingListRow);
            }

            MasterListRowEntity lastRow = existingList.MaxBy(x => x.RowNum)!;

            int rowNum = lastRow.RowNum + 1;

            var actions = request.Rows
                .Select(
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
                )
                .Chunk(100)
                .Select(x => tableClient.SubmitTransactionAsync(x, token));

            Response<IReadOnlyList<Response>>[] responses = await Task.WhenAll(actions);

            MasterListRow[]? updatedList = await GetMasterList(listId, token);
            await UpdateSavedMasterList(listId, null, updatedList.Length, token);

            return updatedList;
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

    // --- Admin Tests (creating, deleting, editing, etc) ---
    public async Task<TestRow[]> CreateTest(CreateTestRequest request, CancellationToken token)
    {
        TableClient testsTableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{TestsTableName}");
        TableClient testOverviewTableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{TestsOverviewTableName}");

        try
        {
            Guid testId = Guid.NewGuid();
            int rowNum = 1;
            DateTimeOffset testCreationTimestamp = DateTimeOffset.UtcNow;
            List<TestRowEntity> rowsToCreate = request.Rows
                .Select(createRowRequest => createRowRequest.AsEntity(testId, request.Name, rowNum++, testCreationTimestamp))
                .ToList();

            var addEntitiesBatches = rowsToCreate
                .Select(x => new TableTransactionAction(TableTransactionActionType.Add, x))
                .Chunk(100)
                .Select(x => testsTableClient.SubmitTransactionAsync(x, token));

            Response<IReadOnlyList<Response>>[] responses = await Task.WhenAll(addEntitiesBatches);

            // Also add an entry to the overview table
            await testOverviewTableClient.AddEntityAsync(
                new TestOverviewEntity
                {
                    TestCreationDate = DateOnly.FromDateTime(testCreationTimestamp.UtcDateTime),
                    TestCreationTimestamp = testCreationTimestamp,
                    TestId = testId,
                    TestName = request.Name,
                    TotalRows = rowsToCreate.Count,
                    IsAvailable = false,
                    IsRetakeable = false,
                    SourceList = request.SourceList,
                    SourceListName = request.SourceListName,
                },
                token
            );

            return rowsToCreate.Select(x => x.AsDTO()).ToArray();
        }
        catch (Exception ex)
        {
            if (ex is TableTransactionFailedException ttfEx)
            {
                string errorFragment = "";
                if (ttfEx.FailedTransactionActionIndex != null)
                {
                    errorFragment =
                        $" Object at index: {JsonSerializer.Serialize(request.Rows[ttfEx.FailedTransactionActionIndex.Value])}";
                }
                _logger.LogError(
                    $"Transaciton failed when creating new table at index: {ttfEx.FailedTransactionActionIndex}.{errorFragment}"
                );

                // TODO: Investigate what kind of errors we catch here.
                throw new StatusCodeException(StatusCodes.Status400BadRequest);
            }

            throw new StatusCodeException(
                StatusCodes.Status500InternalServerError,
                $"Failed to create test with name {request.Name} into Tests table.",
                ex
            );
        }
    }

    public async Task<TestOverviewResponse> GetTestOverview(string? continuationToken, CancellationToken token)
    {
        var tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{TestsOverviewTableName}");
        try
        {
            // We only ever get one page at a time
            var enumerablePages = tableClient.QueryAsync<TestOverviewEntity>(cancellationToken: token).AsPages(continuationToken);
            var pageEnumerator = enumerablePages.GetAsyncEnumerator(token);
            if (!await pageEnumerator.MoveNextAsync())
            {
                return new TestOverviewResponse(Array.Empty<TestOverviewEntry>(), null);
            }

            return new TestOverviewResponse(
                pageEnumerator.Current.Values.Select(x => x.AsOverviewDTO()).ToArray(),
                pageEnumerator.Current.ContinuationToken
            );
        }
        catch (RequestFailedException ex)
        {
            // TODO: oh god this can fail in so many ways
            _logger.LogError("GetTestOverview died. {ex}", ex);
            Debugger.Break(); // look, just go inspect why and construct a sane handler piece by piece as you test this
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, null, ex);
        }
    }

    public async Task<TestOverviewEntry> GetSingleTestOverview(Guid testId, CancellationToken token)
    {
        var tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{TestsOverviewTableName}");
        try
        {
            await foreach (
                TestOverviewEntity? overview in tableClient.QueryAsync<TestOverviewEntity>(
                    x => x.RowKey == testId.ToString(),
                    cancellationToken: token
                )
            )
            {
                if (overview == null)
                {
                    throw new StatusCodeException(StatusCodes.Status404NotFound);
                }

                return overview.AsOverviewDTO();
            }

            // If we make it through the loop without finding anything, it's 404 time
            throw new StatusCodeException(StatusCodes.Status404NotFound);
        }
        catch (RequestFailedException ex)
        {
            // TODO: oh god this can fail in so many ways
            _logger.LogError("GetSingleTestOverview died. {ex}", ex);
            Debugger.Break(); // look, just go inspect why and construct a sane handler piece by piece as you test this
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, null, ex);
        }
    }

    public async Task EditTestOverview(EditTestOverviewRequest request, CancellationToken token)
    {
        var tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{TestsOverviewTableName}");
        DateOnly date = DateOnly.FromDateTime(request.TestCreationTimestamp.Date);
        string partitionKey = TestOverviewEntity.ConvertToPartitionKey(date);

        var updateEntity = new TableEntity(partitionKey, request.TestId.ToString())
        {
            { nameof(TestOverviewEntity.IsAvailable), request.NewAvailable },
            { nameof(TestOverviewEntity.IsRetakeable), request.NewRetakeable },
        };

        try
        {
            _ = await tableClient.UpdateEntityAsync(updateEntity, ETag.All, TableUpdateMode.Merge, token);
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status == StatusCodes.Status404NotFound)
            {
                throw new StatusCodeException(
                    StatusCodes.Status404NotFound,
                    "No test with that ID and timestamp could be found.",
                    ex
                );
            }

            throw new StatusCodeException(StatusCodes.Status500InternalServerError, "Failed to update test.", ex);
        }
    }

    public async Task<TestRowSimple[]> GetTestSimple(Guid testId, CancellationToken token)
    {
        var tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{TestsTableName}");
        try
        {
            AsyncPageable<TestRowEntity> testQuery = tableClient.QueryAsync<TestRowEntity>(
                x => x.PartitionKey == testId.ToString(),
                select: new[] { nameof(TestRowEntity.RowNum), nameof(TestRowEntity.WordsJson) },
                cancellationToken: token
            );

            List<TestRowSimple> testRows = new List<TestRowSimple>();
            await foreach (TestRowEntity testRow in testQuery)
            {
                testRows.Add(testRow.AsSimpleDTO());
            }
            if (!testRows.Any())
            {
                throw new StatusCodeException(StatusCodes.Status404NotFound);
            }

            return testRows.ToArray();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError("Failed to get simple test data. Details: {ex}", ex);
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, null, ex);
        }
    }

    public async Task<TestResultsOverviewRow[]> GetTestResultsOverview(Guid testOrUserId, CancellationToken token)
    {
        var tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{TestResultsOverviewTableName}");
        try
        {
            List<TestResultsOverviewEntity> resultsOverviewList = new();
            await foreach (
                var resultOverview in tableClient.QueryAsync<TestResultsOverviewEntity>(
                    x => x.PartitionKey == testOrUserId.ToString(),
                    cancellationToken: token
                )
            )
            {
                resultsOverviewList.Add(resultOverview);
            }

            return resultsOverviewList.Select(x => x.AsDTO()).ToArray();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError("Failed to get test results overview by test ID. Details: {ex}", ex);
            throw new StatusCodeException(StatusCodes.Status500InternalServerError, null, ex);
        }
    }

    // --- Admin Users ---
    public async Task<AuthenticatedUser> CreateAdminUser(string username, string password, CancellationToken token)
    {
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{AdminUserTableName}");
        AdminUserEntity userEntity = new AdminUserEntity
        {
            PartitionKey = AdminUserEntity.DefaultPartitionKey,
            RowKey = username,
            HashedPassword = Argon2.Hash(password),
        };
        try
        {
            await tableClient.AddEntityAsync(userEntity, token);
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
        TableClient tableClient = _tableServiceClient.GetTableClient($"{_tablePrefix}{AdminUserTableName}");

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

    // Local utilities and helpers

    private async Task UpdateSavedMasterList(Guid listId, string? newName, int? newRowCount, CancellationToken token)
    {
        var savedListclient = _tableServiceClient.GetTableClient($"{_tablePrefix}{SavedMasterListTableName}");
        var entity = new TableEntity(SavedMasterListEntity.DefaultPartitionKey, listId.ToString());
        if (newName != null)
        {
            entity[nameof(SavedMasterListEntity.ListName)] = newName;
        }
        if (newRowCount != null)
        {
            entity[nameof(SavedMasterListEntity.TotalRows)] = newRowCount;
        }

        await savedListclient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Merge, cancellationToken: token);
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
        IEnumerable<MasterListRow> listRows,
        HashSet<Guid> rowsToDelete,
        CancellationToken token
    );

    Task DeleteMasterList(Guid listId, CancellationToken token);

    // Tests
    Task<TestRow[]> CreateTest(CreateTestRequest request, CancellationToken token);
    Task<TestRowSimple[]> GetTestSimple(Guid testId, CancellationToken token);
    Task<TestOverviewEntry> GetSingleTestOverview(Guid testId, CancellationToken token);
    Task<TestOverviewResponse> GetTestOverview(string? continuationToken, CancellationToken token);
    Task<TestResultsOverviewRow[]> GetTestResultsOverview(Guid testOrUserId, CancellationToken token);

    // Admin users
    Task<AuthenticatedUser> CreateAdminUser(string username, string password, CancellationToken token);
    Task<AuthenticatedUser> GetAdminUser(string username, CancellationToken token);
    Task<MasterListRow[]> AddMasterListRows(Guid listId, AddMasterListRowsRequest request, CancellationToken token);
    Task EditTestOverview(EditTestOverviewRequest request, CancellationToken token);
}

using VerbIt.Backend.Extensions;
using VerbIt.Backend.Repositories;
using VerbIt.DataModels;

namespace VerbIt.Backend.Services
{
    public class MasterListService : IMasterListService
    {
        private readonly ILogger<MasterListService> _logger;
        private readonly IVerbitRepository _repository;

        public MasterListService(ILogger<MasterListService> logger, IVerbitRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<SavedMasterList[]> GetSavedMasterLists(CancellationToken token)
        {
            return await _repository.GetSavedMasterLists(token);
        }

        public Task<MasterListRow[]> GetList(Guid listId, CancellationToken token)
        {
            return _repository.GetMasterList(listId, token);
        }

        public Task<MasterListRow[]> CreateList(CreateMasterListRequest createRequest, CancellationToken token)
        {
            return _repository.CreateMasterList(createRequest, token);
        }

        // TODO: List with two items, deleted item 1, didn't see row adjustments. Fix this.

        public async Task<MasterListRow[]> DeleteRows(
            Guid listId,
            DeleteMasterListRowsRequest deleteRequest,
            CancellationToken token
        )
        {
            MasterListRow[] existingList = await _repository.GetMasterList(listId, token);

            if (
                deleteRequest.RowIds.Length == existingList.Length
                && deleteRequest.RowIds.All(existingList.Select(x => x.RowId).Contains)
            )
            {
                // The user just wants to delete everything. Kay.
                await _repository.DeleteMasterList(listId, token);
                return Array.Empty<MasterListRow>();
            }

            HashSet<Guid> toDeleteHashes = deleteRequest.RowIds.ToHashSet();
            var contiguousGroups = existingList
                .Where(row => toDeleteHashes.Contains(row.RowId))
                .OrderBy(x => x.RowNum)
                .GroupContiguousBy(x => x.RowNum);

            // Iterate through contiguous sublists in reverse order
            // For each one:
            // - Get the index of their last element
            // - Every element in the original list BEYOND that index will have its rownum decremented by the sublist's Count
            foreach (var deleteGroup in contiguousGroups.Reverse())
            {
                List<MasterListRow> realizedDeleteGroup = deleteGroup.ToList();
                int indexOfLast = Array.IndexOf(existingList, realizedDeleteGroup.Last());
                if (indexOfLast == existingList.Length - 1)
                {
                    // This deleteGroup contains the last element in the list. Nothing beyond to adjust.
                    continue;
                }

                for (int i = indexOfLast + 1; i < existingList.Length; i++)
                {
                    MasterListRow rowToAdjust = existingList[i];
                    existingList[i] = rowToAdjust with { RowNum = rowToAdjust.RowNum - realizedDeleteGroup.Count };
                }
            }

            return await _repository.DeleteMasterListRows(listId, existingList, toDeleteHashes, token);
        }

        public Task DeleteList(Guid listId, CancellationToken token)
        {
            return _repository.DeleteMasterList(listId, token);
        }

        public async Task<MasterListRow[]> EditList(Guid listId, EditMasterListRequest editRequest, CancellationToken token)
        {
            List<Guid>? rowIdsToChangeName = null;
            // First, get existing list
            MasterListRow[] existingList = await _repository.GetMasterList(listId, token);

            if (editRequest.Rows == null || !editRequest.Rows.Any())
            {
                if (editRequest.ListName != null)
                {
                    rowIdsToChangeName = existingList.Select(x => x.RowId).ToList();
                }
            }
            else
            {
                // If we have any rows that aren't in the existing list, tell the user to try harder
                HashSet<Guid> existingRows = existingList.Select(x => x.RowId).ToHashSet();
                if (!editRequest.Rows.Select(x => x.RowId).All(existingRows.Contains))
                {
                    throw new StatusCodeException(
                        StatusCodes.Status400BadRequest,
                        "All Row IDs included in an edit request must already exist in the list."
                    );
                }

                // If name is set, get all the existing rows that otherwise don't need to be changed, and
                // note them down
                if (editRequest.ListName != null)
                {
                    rowIdsToChangeName = existingList.Select(x => x.RowId).Except(editRequest.Rows.Select(y => y.RowId)).ToList();
                }

                // Weave edited rowNums into it for each row that has one
                foreach (EditMasterListRowRequest editRow in editRequest.Rows.Where(x => x.RowNum != null))
                {
                    var existingRow = existingList.FirstOrDefault(x => x.RowId == editRow.RowId);
                    if (existingRow == null)
                    {
                        throw new StatusCodeException(
                            StatusCodes.Status400BadRequest,
                            $"Row with ID {editRow.RowId} was not found in this Master List."
                        );
                    }

                    existingList[Array.IndexOf(existingList, existingRow)] = existingRow with { RowNum = editRow.RowNum!.Value };
                }

                // Check for dupes, and yell if there are any
                if (existingList.DistinctBy(x => x.RowNum).Count() != existingList.Count())
                {
                    throw new StatusCodeException(
                        StatusCodes.Status400BadRequest,
                        "A row cannot be edited to have the same RowNum as another row."
                    );
                }
            }

            await _repository.EditMasterList(listId, editRequest, rowIdsToChangeName?.ToArray(), token);

            // Return the now-edited list
            return await _repository.GetMasterList(listId, token);
        }

        public async Task<MasterListRow[]> AddRows(Guid listId, AddMasterListRowsRequest request, CancellationToken token)
        {
            return await _repository.AddMasterListRows(listId, request, token);
        }
    }

    public interface IMasterListService
    {
        Task<SavedMasterList[]> GetSavedMasterLists(CancellationToken token);
        Task<MasterListRow[]> GetList(Guid listId, CancellationToken token);
        Task<MasterListRow[]> CreateList(CreateMasterListRequest createRequest, CancellationToken token);
        Task<MasterListRow[]> DeleteRows(Guid listId, DeleteMasterListRowsRequest deleteRequest, CancellationToken token);
        Task DeleteList(Guid listId, CancellationToken token);
        Task<MasterListRow[]> EditList(Guid listId, EditMasterListRequest editRequest, CancellationToken token);
        Task<MasterListRow[]> AddRows(Guid listId, AddMasterListRowsRequest addRequest, CancellationToken token);
    }
}

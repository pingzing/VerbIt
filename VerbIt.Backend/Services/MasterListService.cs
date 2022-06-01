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

        public Task<MasterListRow[]> GetMasterList(Guid listId, CancellationToken token)
        {
            return _repository.GetMasterList(listId, token);
        }

        public Task<MasterListRow[]> CreateMasterList(CreateMasterListRequest createRequest, CancellationToken token)
        {
            return _repository.CreateMasterList(createRequest, token);
        }

        public async Task<MasterListRow[]> DeleteRows(DeleteMasterListRowsRequest deleteRequest, CancellationToken token)
        {
            HashSet<Guid> toDeleteHashes = deleteRequest.RowIds.ToHashSet();
            MasterListRow[] existingList = await _repository.GetMasterList(deleteRequest.ListId, token);
            existingList.Where(row => toDeleteHashes.Contains(row.RowId));
        }

        public async Task<MasterListRow[]> EditMasterList(EditMasterListRequest editRequest, CancellationToken token)
        {
            // First, get existing list
            MasterListRow[] existingList = await _repository.GetMasterList(editRequest.ListId, token);

            // If name is set, get all the existing rows that otherwise don't need to be changed, and
            // note them down
            List<Guid>? rowIdsToChangeName = null;
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

            await _repository.EditMasterList(editRequest, rowIdsToChangeName?.ToArray(), token);

            // Return the now-edited list
            return await _repository.GetMasterList(editRequest.ListId, token);
        }
    }

    public interface IMasterListService
    {
        Task<MasterListRow[]> GetMasterList(Guid listId, CancellationToken token);
        Task<MasterListRow[]> CreateMasterList(CreateMasterListRequest createRequest, CancellationToken token);
        Task<MasterListRow[]> DeleteRows(DeleteMasterListRowsRequest deleteRequest, CancellationToken token);
        Task<MasterListRow[]> EditMasterList(EditMasterListRequest editRequest, CancellationToken token);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerbIt.Backend.Services;
using VerbIt.DataModels;

namespace VerbIt.Backend.Controllers
{
    [ApiController]
    [Route("api/masterlist")]
    [Authorize(Roles = VerbitRoles.Admin)]
    public class MasterListController : ControllerBase
    {
        private readonly ILogger<MasterListController> _logger;
        private readonly IMasterListService _masterListService;

        public MasterListController(ILogger<MasterListController> logger, IMasterListService masterListService)
        {
            _logger = logger;
            _masterListService = masterListService;
        }

        [HttpGet]
        [Route("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SavedMasterList[]))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SavedMasterList[]>> GetSavedLists(CancellationToken token)
        {
            return await _masterListService.GetSavedMasterLists(token);
        }

        [HttpGet]
        [Route("{listId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> Get(Guid listId, CancellationToken token)
        {
            MasterListRow[] rows = await _masterListService.GetList(listId, token);
            if (!rows.Any())
            {
                return NotFound();
            }

            return rows;
        }

        [HttpPost]
        [Route("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> Create(CreateMasterListRequest createRequest, CancellationToken token)
        {
            return await _masterListService.CreateList(createRequest, token);
        }

        [HttpDelete]
        [Route("{listId}/deleterows")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MasterListRow[]))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> DeleteRows(
            Guid listId,
            DeleteMasterListRowsRequest deleteRowsRequest,
            CancellationToken token
        )
        {
            return await _masterListService.DeleteRows(listId, deleteRowsRequest, token);
        }

        [HttpDelete]
        [Route("{listId}/delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteList(Guid listId, CancellationToken token)
        {
            await _masterListService.DeleteList(listId, token);
            return Ok();
        }

        [HttpPatch]
        [Route("{listId}/edit")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MasterListRow[]))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> Edit(
            Guid listId,
            EditMasterListRequest editRequest,
            CancellationToken token
        )
        {
            EditMasterListRowRequest? invalidRow = editRequest.Rows?.FirstOrDefault(x => x.RowNum == null && x.Words == null);
            if (invalidRow != null)
            {
                return BadRequest(
                    $"All rows must have at either a RowNum or a Words array. RowId {invalidRow.RowId} had neither."
                );
            }

            MasterListRow[] newRows = await _masterListService.EditList(listId, editRequest, token);
            return Ok(newRows);
        }

        [HttpPatch]
        [Route("{listId}/addrows")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MasterListRow[]))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> AddRows(
            Guid listId,
            AddMasterListRowsRequest addRequest,
            CancellationToken token
        )
        {
            return await _masterListService.AddRows(listId, addRequest, token);
        }
    }
}

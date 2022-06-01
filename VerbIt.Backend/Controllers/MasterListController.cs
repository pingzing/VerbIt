using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerbIt.Backend.Repositories;
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
        [Route("{masterListId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> Get(Guid masterListId, CancellationToken token)
        {
            return await _masterListService.GetMasterList(masterListId, token);
        }

        [HttpPost]
        [Route("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> Create(CreateMasterListRequest createRequest, CancellationToken token)
        {
            return await _masterListService.CreateMasterList(createRequest, token);
        }

        [HttpDelete]
        [Route("deleterows")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteRows(DeleteMasterListRowsRequest deleteRowsRequest, CancellationToken token)
        {
            _masterListService.DeleteRows(deleteRowsRequest, token);
        }

        [HttpPatch]
        [Route("edit")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MasterListRow[]))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> Edit(EditMasterListRequest editRequest, CancellationToken token)
        {
            EditMasterListRowRequest? invalidRow = editRequest.Rows.FirstOrDefault(x => x.RowNum == null && x.Words == null);
            if (invalidRow != null)
            {
                return BadRequest(
                    $"All rows must have at either a RowNum or a Words array. RowId {invalidRow.RowId} had neither."
                );
            }

            MasterListRow[] newRows = await _masterListService.EditMasterList(editRequest, token);
            return Ok(newRows);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerbIt.Backend.Repositories;
using VerbIt.DataModels;

namespace VerbIt.Backend.Controllers
{
    [ApiController]
    [Route("api/masterlist")]
    [Authorize(Roles = VerbitRoles.Admin)]
    public class MasterListController : ControllerBase
    {
        private readonly ILogger<MasterListController> _logger;
        private readonly IVerbitRepository _repository;

        public MasterListController(ILogger<MasterListController> logger, IVerbitRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpPost]
        [Route("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MasterListRow[]>> Create(MasterListRow[] newList, CancellationToken token)
        {
            string listName = newList[0].Name;
            if (!newList.All(x => x.Name == listName))
            {
                return BadRequest("All rows in the list must have the same list name.");
            }

            if (newList.DistinctBy(x => x.Number).Count() != newList.Length)
            {
                return BadRequest("All rows in the list must have unique row numbers.");
            }

            return await _repository.CreateMasterList(newList, token);
        }

        [HttpPut]
        [Route("edit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Edit(EditMasterListRequest editRequest, CancellationToken token)
        {
            await _repository.EditMasterList(editRequest, token);
            return Ok();
        }

        [HttpGet]
        [Route("{masterListName}")]
        public async Task<ActionResult<MasterListRow[]>> Get(string masterListName, CancellationToken token)
        {
            throw new NotImplementedException();
            //return await _repository.GetMasterList(masterListName, token);
        }
    }
}

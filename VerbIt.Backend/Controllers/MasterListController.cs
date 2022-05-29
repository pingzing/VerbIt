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
        public async Task<IActionResult> Create(MasterList newList, CancellationToken token)
        {
            await _repository.CreateMasterList(newList, token);
            return Ok();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerbIt.Backend.Repositories;
using VerbIt.DataModels;

namespace VerbIt.Backend.Controllers
{
    [ApiController]
    [Route("api/adminuser")]
    [Authorize(Roles = VerbitRoles.Admin)]
    public class AdminUserController : ControllerBase
    {
        private readonly ILogger<MasterListController> _logger;
        private readonly IVerbitRepository _repository;

        public AdminUserController(ILogger<MasterListController> logger, IVerbitRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpPost]
        [Route("")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AuthenticatedUser))]
        public async Task<ActionResult<AuthenticatedUser>> Create(
            CreateAdminUserRequest request,
            CancellationToken token
        )
        {
            AuthenticatedUser createdUser = await _repository.CreateAdminUser(
                request.Username,
                request.Password,
                token
            );
            return CreatedAtAction(nameof(AdminUserController.Get), new { username = createdUser.Name }, createdUser);
        }

        [HttpGet]
        [Authorize(Roles = VerbitRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("{username}")]
        public async Task<ActionResult<AuthenticatedUser>> Get(string username, CancellationToken token)
        {
            return await _repository.GetAdminUser(username, token);
        }
    }
}

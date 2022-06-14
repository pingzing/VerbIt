using Microsoft.AspNetCore.Mvc;

namespace VerbIt.Backend.Controllers;

[ApiController]
[Route("api/tests")]
public class UserTestsController : ControllerBase
{
    private readonly ILogger<UserTestsController> _logger;

    public UserTestsController(ILogger<UserTestsController> logger)
    {
        _logger = logger;
    }
}

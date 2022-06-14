using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VerbIt.Backend.Services;
using VerbIt.DataModels;

namespace VerbIt.Backend.Controllers;

[ApiController]
[Route("api/tests")]
[Authorize(Roles = VerbitRoles.Admin)]
public class AdminTestsController : ControllerBase
{
    private readonly ILogger<AdminTestsController> _logger;
    private readonly ITestsService _testsService;

    public AdminTestsController(ILogger<AdminTestsController> logger, ITestsService testsService)
    {
        _logger = logger;
        _testsService = testsService;
    }

    [HttpGet]
    public async Task<ActionResult<string>> GetTest()
    {
        return "hi!";
    }
}

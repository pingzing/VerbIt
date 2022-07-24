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

    [HttpPost]
    [Route("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestRow[]>> CreateTest(CreateTestRequest request, CancellationToken token)
    {
        var createdTest = await _testsService.CreateTest(request, token);
        return CreatedAtAction(nameof(AdminTestsController.GetTestDetails), new { testId = createdTest[0].TestId }, createdTest);
    }

    [HttpGet]
    [Route("overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TestOverviewResponse>> GetTestOverview(
        [FromQuery(Name = "con")] string? continuationToken,
        CancellationToken token
    )
    {
        return await _testsService.GetTestOverview(continuationToken, token);
    }

    [HttpGet]
    [Route("{testId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<TestWithResults> GetTestDetails(Guid testId, CancellationToken token)
    {
        return await _testsService.GetTestWithResults(testId, token);
    }

    [HttpPatch]
    [Route("overview/{testId}/edit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> EditTestOverview(EditTestOverviewRequest request, CancellationToken token)
    {
        await _testsService.EditTestOverview(request, token);
        return Ok();
    }
}

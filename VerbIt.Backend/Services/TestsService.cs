using VerbIt.Backend.Repositories;
using VerbIt.DataModels;

namespace VerbIt.Backend.Services;

public class TestsService : ITestsService
{
    private readonly ILogger<TestsService> _logger;
    private readonly IVerbitRepository _repository;

    public TestsService(ILogger<TestsService> logger, IVerbitRepository repository)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<TestRow[]> CreateTest(CreateTestRequest request, CancellationToken token)
    {
        return _repository.CreateTest(request, token);
    }

    // Intended for on a "Tests" landing page that shows a pageable list of all tests
    public Task<TestOverviewResponse> GetTestOverview(string? continuationToken, CancellationToken token)
    {
        return _repository.GetTestOverview(continuationToken, token);
    }

    // Displayed to the admin user when they click on a test
    public async Task<TestWithResults> GetTestWithResults(Guid testId, CancellationToken token)
    {
        var testOverviewTask = _repository.GetSingleTestOverview(testId, token);
        var simpleQuestionsTask = _repository.GetTestSimple(testId, token);
        var resultsOverviewTask = _repository.GetTestResultsOverview(testId, token);

        await Task.WhenAll(testOverviewTask, simpleQuestionsTask, resultsOverviewTask);

        return new TestWithResults(
            testId,
            testOverviewTask.Result.TestName,
            testOverviewTask.Result.TestCreationTimestamp,
            testOverviewTask.Result.IsAvailable,
            testOverviewTask.Result.IsRetakeable,
            testOverviewTask.Result.SourceList,
            testOverviewTask.Result.SourceListName,
            simpleQuestionsTask.Result.OrderBy(x => x.RowNum).ToArray(),
            resultsOverviewTask.Result
        );
    }

    public async Task EditTestOverview(EditTestOverviewRequest request, CancellationToken token)
    {
        await _repository.EditTestOverview(request, token);
    }
}

public interface ITestsService
{
    Task<TestRow[]> CreateTest(CreateTestRequest request, CancellationToken token);
    Task<TestWithResults> GetTestWithResults(Guid testId, CancellationToken token);
    Task<TestOverviewResponse> GetTestOverview(string? continuationToken, CancellationToken token);
    Task EditTestOverview(EditTestOverviewRequest request, CancellationToken token);
}

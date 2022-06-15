using Microsoft.AspNetCore.Mvc;
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

    public Task<TestOverviewResponse> GetTestOverview(string? continuationToken, CancellationToken token)
    {
        return _repository.GetTestOverview(continuationToken, token);
    }

    public Task<TestWithResults> GetTestWithResults(Guid testId, CancellationToken token)
    {
        // Get the questions about a test, and all the test results that exist for this test
    }
}

public interface ITestsService
{
    Task<TestRow[]> CreateTest(CreateTestRequest request, CancellationToken token);
    Task<TestWithResults> GetTestDetails(Guid testId, CancellationToken token);
    Task<TestOverviewResponse> GetTestOverview(string? continuationToken, CancellationToken token);
}

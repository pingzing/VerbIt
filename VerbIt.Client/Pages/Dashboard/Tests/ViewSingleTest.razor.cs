using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.Tests
{
    public partial class ViewSingleTest : ComponentBase
    {
        [CascadingParameter]
        private DashboardLayout Layout { get; set; } = null!;

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        [Parameter]
        public string TestId { get; set; } = null!;
        private bool _testExists = false;
        private Guid? _testId = null;

        private string? TestName { get; set; } = null;
        private List<TestRowSimple> TestQuestions { get; set; } = new List<TestRowSimple>();

        protected override async Task OnInitializedAsync()
        {
            Layout.Title = $"Tests - Loading...";
            Layout.BackButtonText = "↑ Go up to Tests";

            if (!Guid.TryParse(TestId, out Guid testId))
            {
                return;
            }

            _testId = testId;

            TestWithResults? test = await _networkService.GetTestDetails(_testId.Value, CancellationToken.None);
            if (test == null)
            {
                return;
            }

            // Init properties for razor to see
            _testExists = true;
            Layout.Title = $"Tests - {test.TestName}";
            TestName = test.TestName;
            TestQuestions = new List<TestRowSimple>(test.Questions);
        }
    }
}

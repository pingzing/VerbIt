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
        private Guid? _testId = null;

        private TestWithResults? TestData { get; set; } = null;

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
            Layout.Title = $"Tests - {test.TestName}";
            TestData = test;
        }
    }
}

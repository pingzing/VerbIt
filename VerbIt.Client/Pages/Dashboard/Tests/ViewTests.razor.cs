using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.Tests
{
    public partial class ViewTests : ComponentBase
    {
        [CascadingParameter]
        private IModalService _modalService { get; set; } = null!;

        [CascadingParameter]
        private DashboardLayout Layout { get; set; } = null!;

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private ILogger<ViewTests> _logger { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        private List<TestOverviewEntry> TestsList { get; set; } = new List<TestOverviewEntry>();

        protected override async Task OnInitializedAsync()
        {
            Layout.Title = "Tests";
            Layout.BackButtonText = "↑ Go up to Dashboard";

            TestsList.Clear();
            string? continuationToken = null;
            do
            {
                var getTestsOverviewReuslt = await _networkService.GetTestOverview(continuationToken, CancellationToken.None);
                if (getTestsOverviewReuslt == null)
                {
                    // TODO: Show error
                    return;
                }
                continuationToken = getTestsOverviewReuslt.ContinuationToken;
                TestsList.AddRange(getTestsOverviewReuslt.OverviewEntries);
            } while (continuationToken != null);
        }

        private async Task DeleteTest(TestOverviewEntry testToDelete) { }
    }
}

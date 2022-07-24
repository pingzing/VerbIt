using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using VerbIt.Client.Models;
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

        private List<TestOverviewEntryVM> TestsList { get; set; } = new List<TestOverviewEntryVM>();

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
                TestsList.AddRange(getTestsOverviewReuslt.OverviewEntries.Select(x => new TestOverviewEntryVM(x)));
            } while (continuationToken != null);
        }

        private async Task AvailableChanged(TestOverviewEntryVM changedEntry)
        {
            // TODO: Ask are you sure?
            changedEntry.IsAvailable = !changedEntry.IsAvailable;
            await CallEditOverview(changedEntry, CancellationToken.None);
        }

        private async Task RetakeableChanged(TestOverviewEntryVM changedEntry)
        {
            // TODO: Ask are you sure?
            changedEntry.IsRetakeable = !changedEntry.IsRetakeable;
            await CallEditOverview(changedEntry, CancellationToken.None);
        }

        private async Task CallEditOverview(TestOverviewEntryVM changedEntry, CancellationToken token)
        {
            // TODO: Show loading indicator, disable row

            var editRequest = new EditTestOverviewRequest(
                changedEntry.TestCreationTimestamp,
                changedEntry.TestId,
                changedEntry.IsAvailable,
                changedEntry.IsRetakeable
            );
            bool success = await _networkService.EditTestOverview(editRequest, CancellationToken.None);
            if (!success)
            {
                // TODO: Hide loading indicator, reenable row
                // TODO: Show sadness
                return;
            }

            // TODO: Hide loading indicator, reenable checkbox
        }

        private async Task DeleteTest(TestOverviewEntryVM testToDelete) { }
    }
}

using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using VerbIt.Client.Components;
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

        private LoadingState GetTestLoadingState { get; set; } = LoadingState.NotStarted;
        private List<TestOverviewEntryVM> TestsList { get; set; } = new List<TestOverviewEntryVM>();

        protected override async Task OnInitializedAsync()
        {
            Layout.Title = "Tests";
            Layout.BackButtonText = "↑ Go up to Dashboard";

            GetTestLoadingState = LoadingState.Loading;

            TestsList.Clear();
            string? continuationToken = null;
            do
            {
                var getTestsOverviewReuslt = await _networkService.GetTestOverview(continuationToken, CancellationToken.None);
                if (getTestsOverviewReuslt == null)
                {
                    GetTestLoadingState = LoadingState.Failure;
                    return;
                }
                continuationToken = getTestsOverviewReuslt.ContinuationToken;
                TestsList.AddRange(getTestsOverviewReuslt.OverviewEntries.Select(x => new TestOverviewEntryVM(x)));
            } while (continuationToken != null);

            GetTestLoadingState = LoadingState.Success;
        }

        private async Task AvailableChanged(TestOverviewEntryVM changedEntry)
        {
            // TODO: Ask are you sure?
            changedEntry.IsAvailable = !changedEntry.IsAvailable;
            await EditOverview(changedEntry, CancellationToken.None);
        }

        private async Task RetakeableChanged(TestOverviewEntryVM changedEntry)
        {
            // TODO: Ask are you sure?
            changedEntry.IsRetakeable = !changedEntry.IsRetakeable;
            await EditOverview(changedEntry, CancellationToken.None);
        }

        private async Task EditOverview(TestOverviewEntryVM changedEntry, CancellationToken token)
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

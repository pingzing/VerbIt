using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VerbIt.Client.Components;
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

        [Parameter]
        public string? Timestamp { get; set; } = null!;
        private DateTimeOffset? _testCreationTimestamp = null;

        private LoadingState TestDetailsLoadingState { get; set; } = LoadingState.NotStarted;
        private TestWithResults? TestData { get; set; } = null;
        private bool IsAvailable { get; set; }
        private bool IsRetakeable { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Layout.Title = $"Tests - Loading...";
            Layout.BackButtonText = "↑ Go up to Tests";

            if (!Guid.TryParse(TestId, out Guid testId))
            {
                TestDetailsLoadingState = LoadingState.Failure;
                return;
            }

            _testId = testId;

            if (!DateTimeOffset.TryParse(Timestamp, out DateTimeOffset testCreationTimestamp))
            {
                TestDetailsLoadingState = LoadingState.Failure;
                return;
            }

            _testCreationTimestamp = testCreationTimestamp;

            // Remove the timestamp from the URL, so it doesn't mess with our
            // "go up" button, or clutter up the address bar
            NavManager.NavigateTo(NavManager.Uri.Replace(Timestamp, ""), false, replace: true);

            TestDetailsLoadingState = LoadingState.Loading;
            TestWithResults? test = await _networkService.GetTestDetails(_testId.Value, CancellationToken.None);
            if (test == null)
            {
                TestDetailsLoadingState = LoadingState.Failure;
                return;
            }
            TestDetailsLoadingState = LoadingState.Success;

            // Init properties for razor to see
            Layout.Title = $"Tests - {test.TestName}";
            TestData = test;
            IsAvailable = TestData.IsAvailable;
            IsRetakeable = TestData.IsRetakeable;
        }

        private async Task IsAvailableChanged(ChangeEventArgs e)
        {
            bool newValue = (bool)e.Value;
            IsAvailable = newValue;
            await Task.Yield(); // Force asynchrony to allow the renderer time to see the new value

            bool result = await EditOverview(CancellationToken.None, newValue, null);
            if (!result)
            {
                IsAvailable = !newValue;
            }
        }

        private async Task IsRetakeableChanged(ChangeEventArgs e)
        {
            bool newValue = (bool)e.Value;
            IsRetakeable = newValue;
            await Task.Yield(); // Force asynchrony to allow the renderer time to see the new value

            bool result = await EditOverview(CancellationToken.None, null, newValue);
            if (!result)
            {
                IsRetakeable = !newValue;
            }
        }

        private async Task<bool> EditOverview(CancellationToken token, bool? newAvailable = null, bool? newRetakeable = null)
        {
            return await _networkService.EditTestOverview(
                new EditTestOverviewRequest(_testCreationTimestamp!.Value, _testId!.Value, newAvailable, newRetakeable),
                token
            );
        }
    }
}

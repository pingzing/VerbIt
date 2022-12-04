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

            // TODO: Network request to set it
            // On failure:
            IsAvailable = !newValue;
        }

        private async Task IsRetakeableChanged(ChangeEventArgs e)
        {
            bool newValue = (bool)e.Value;
            IsRetakeable = newValue;
            await Task.Yield(); // Force asynchrony to allow the renderer time to see the new value

            // TODO: Network request to set it
            // On failure:
            IsRetakeable = !newValue;
        }
    }
}

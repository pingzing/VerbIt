using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using VerbIt.Client.Models;

namespace VerbIt.Client.Pages.Dashboard.Tests
{
    public partial class CreateTest : ComponentBase
    {
        [CascadingParameter]
        private DashboardLayout Layout { get; set; } = null!;

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        private string TestNameFieldClass { get; set; } = "";
        private string TestName { get; set; } = "";
        private bool IsSaving { get; set; } = false;
        private List<CreateTestRowVM> RowList { get; set; } = new List<CreateTestRowVM>();

        protected override Task OnInitializedAsync()
        {
            Layout.Title = "Tests - Create";
            Layout.BackButtonText = "↑ Go up to Tests";

            var thisUri = NavManager.ToAbsoluteUri(NavManager.Uri);

            // TODO: How do we get to this page? Do we require the user to have already selected
            // a master list to derive from? Or does this page give them a list to pick from?

            return Task.CompletedTask;
        }

        internal void AddRowClicked() { }

        internal void SaveListClicked() { }

        internal void OnTestNameInput() { }
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using VerbIt.Client.Models;
using VerbIt.DataModels;

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

        private bool IsListPickerVisible { get; set; } = false;
        private Guid? ChosenMasterList { get; set; } = null;
        private List<SavedMasterList>? SavedMasterLists = null;
        private List<CreateTestRowVM> RowList { get; set; } = new List<CreateTestRowVM>();

        protected override Task OnInitializedAsync()
        {
            Layout.Title = "Tests - Create";
            Layout.BackButtonText = "↑ Go up to Tests";

            var thisUri = NavManager.ToAbsoluteUri(NavManager.Uri);

            if (QueryHelpers.ParseNullableQuery(thisUri.Query)?.TryGetValue("masterList", out var chosenListString) == true)
            {
                if (Guid.TryParse(chosenListString, out Guid chosenListGuid))
                {
                    ChosenMasterList = chosenListGuid;
                }
            }

            // If the user already has a list set,
            // don't show the picker, but try to fetch it.
            if (ChosenMasterList != null)
            {
                IsListPickerVisible = false;

                // Somewhere after this (inline? in a method?) try to fetch the list.
                // If it exists and we get it back display it.
            }
            else
            {
                // If we DON'T, show the masterListSelector, and prompt the user to pick one.
                IsListPickerVisible = true;
            }

            return Task.CompletedTask;
        }

        internal void MasterListClicked(SavedMasterList clickedList) { }

        internal void AddRowClicked() { }

        internal void SaveListClicked() { }

        internal void OnTestNameInput() { }
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using VerbIt.Client.Models;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.Tests
{
    public partial class CreateTest : ComponentBase
    {
        [CascadingParameter]
        private DashboardLayout Layout { get; set; } = null!;

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        private string TestNameFieldClass { get; set; } = "";
        private string TestName { get; set; } = "";
        private bool IsSaving { get; set; } = false;

        private bool IsListSelectorVisible { get; set; } = false;
        private Guid? ChosenMasterList { get; set; } = null;
        private string? ChosenMasterListName { get; set; } = null;
        private List<SelectListRowVM>? ChosenMasterListRows { get; set; } = null;
        private List<SavedMasterList>? SavedMasterLists { get; set; } = null;
        private List<CreateTestRowVM> TestRows { get; set; } = new List<CreateTestRowVM>();

        protected override async Task OnInitializedAsync()
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
                IsListSelectorVisible = false;

                // Somewhere after this (inline? in a method?) try to fetch the list.
                // If it exists and we get it back display it.
                await FetchAndDisplayMasterList(ChosenMasterList.Value, CancellationToken.None);
            }
            else
            {
                // If we DON'T, show the masterListSelector, and prompt the user to pick one.
                IsListSelectorVisible = true;
                var savedMasterLists = await _networkService.GetMasterLists(CancellationToken.None);
                if (savedMasterLists == null)
                {
                    // TODO: Display sadness
                    return;
                }

                SavedMasterLists = savedMasterLists.ToList();
            }
        }

        internal async void MasterListClicked(SavedMasterList clickedList)
        {
            await FetchAndDisplayMasterList(clickedList.ListId, CancellationToken.None);
        }

        internal async Task FetchAndDisplayMasterList(Guid listId, CancellationToken token)
        {
            var fetchedList = await _networkService.GetMasterList(listId, CancellationToken.None);
            if (fetchedList == null)
            {
                // TODO: Show error, and be sad
                return;
            }

            ChosenMasterList = listId;
            ChosenMasterListName = fetchedList[0].ListName;
            ChosenMasterListRows = fetchedList
                .Select(
                    x =>
                        new SelectListRowVM
                        {
                            ListId = x.ListId,
                            RowId = x.RowId,
                            RowNum = x.RowNum,
                            Words = x.Words,
                            IsSelected = false
                        }
                )
                .ToList();

            // Changing the property only triggers a re-render the second time for some reason.
            // So, force it with a manual StateHasChanged().
            IsListSelectorVisible = false;
            StateHasChanged();
        }

        internal void SaveListClicked() { }

        internal void OnTestNameInput() { }
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        private List<SelectListRowVM> TestRows { get; set; } = new List<SelectListRowVM>();

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

        SelectListRowVM? _previouslyClicked = null;

        // If the user just clicked the row. IsSelected must be set manually.
        internal void ChosenMasterListRowClicked(SelectListRowVM clickedRow, MouseEventArgs args)
        {
            if (clickedRow.IsSelected)
            {
                if (args.ShiftKey && _previouslyClicked != null)
                {
                    // If Shift is held down, remove all selected rows between this and _previouslyClicked
                    int smallerIndex = Math.Min(
                        ChosenMasterListRows!.IndexOf(_previouslyClicked),
                        ChosenMasterListRows.IndexOf(clickedRow)
                    );
                    int largerIndex = Math.Max(
                        ChosenMasterListRows!.IndexOf(_previouslyClicked),
                        ChosenMasterListRows.IndexOf(clickedRow)
                    );
                    var deselectionTargets = ChosenMasterListRows.ToArray()[smallerIndex..(largerIndex + 1)].Where(
                        x => x.IsSelected
                    );
                    foreach (SelectListRowVM toDeselect in deselectionTargets)
                    {
                        RemoveRowFromTest(toDeselect);
                        toDeselect.IsSelected = false;
                    }
                }
                else
                {
                    // Single selection
                    RemoveRowFromTest(clickedRow);
                    clickedRow.IsSelected = false;
                }
            }
            else
            {
                // If Shift is held down, add all the unselected rows between this and _previouslyClciked
                if (args.ShiftKey && _previouslyClicked != null)
                {
                    int smallerIndex = Math.Min(
                        ChosenMasterListRows!.IndexOf(_previouslyClicked),
                        ChosenMasterListRows.IndexOf(clickedRow)
                    );
                    int largerIndex = Math.Max(
                        ChosenMasterListRows!.IndexOf(_previouslyClicked),
                        ChosenMasterListRows.IndexOf(clickedRow)
                    );
                    var selectionTargets = ChosenMasterListRows.ToArray()[smallerIndex..(largerIndex + 1)].Where(
                        x => x.IsSelected is not true
                    );
                    foreach (SelectListRowVM toSelect in selectionTargets)
                    {
                        toSelect.IsSelected = true;
                        AddRowToTest(toSelect);
                    }
                }
                else
                {
                    // Single selection
                    clickedRow.IsSelected = true;
                    AddRowToTest(clickedRow);
                }
            }

            _previouslyClicked = clickedRow;
        }

        private void AddRowToTest(SelectListRowVM newRow)
        {
            TestRows.Add(newRow);
        }

        private void RemoveRowFromTest(SelectListRowVM removedRow)
        {
            var existingRow = TestRows.FirstOrDefault(x => x.RowId == removedRow.RowId);
            if (existingRow != null)
            {
                TestRows.Remove(existingRow);
            }
        }

        internal void SaveListClicked() { }

        internal void OnTestNameInput() { }
    }
}

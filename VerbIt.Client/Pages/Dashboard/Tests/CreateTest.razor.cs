using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        private ILogger<CreateTest> _logger { get; set; } = null!;

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        private string TestNameFieldClass { get; set; } = "";
        private string TestName { get; set; } = "";
        private bool IsSaving { get; set; } = false;
        private string SaveErrorString { get; set; } = "";
        private bool IsSaveErrorVisible { get; set; } = false;

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

            IsListSelectorVisible = true;
            var savedMasterLists = await _networkService.GetMasterLists(CancellationToken.None);
            if (savedMasterLists == null)
            {
                // TODO: Display sadness
                return;
            }

            SavedMasterLists = savedMasterLists.ToList();
        }

        internal async void MasterListClicked(SavedMasterList clickedList)
        {
            await FetchAndDisplayMasterList(clickedList.ListId, CancellationToken.None);
        }

        internal async Task FetchAndDisplayMasterList(Guid listId, CancellationToken token)
        {
            var fetchedList = await _networkService.GetMasterList(listId, token);
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
                        TestRows.Add(new CreateTestRowVM(toSelect));
                    }
                }
                else
                {
                    // Single selection
                    clickedRow.IsSelected = true;
                    TestRows.Add(new CreateTestRowVM(clickedRow));
                }
            }

            _previouslyClicked = clickedRow;
        }

        private void RemoveRowFromTest(SelectListRowVM removedRow)
        {
            var existingRow = TestRows.FirstOrDefault(x => x.RowId == removedRow.RowId);
            if (existingRow != null)
            {
                TestRows.Remove(existingRow);
            }
        }

        private void TestRowColumnClicked(CreateTestRowVM row, int columnIndex)
        {
            // If index is hidden: just unhide the one
            if (row.HiddenColumnIndices.Contains(columnIndex))
            {
                row.HiddenColumnIndices.Remove(columnIndex);
            }
            // If index is not hidden: hide all others
            else
            {
                row.HiddenColumnIndices.Clear();
                for (int i = 0; i < row.Words.Length; i++)
                {
                    if (i == columnIndex)
                    {
                        continue;
                    }
                    row.HiddenColumnIndices.Add(i);
                }
            }
        }

        private void TestRowUpClicked(CreateTestRowVM upRow)
        {
            int rowIndex = TestRows.IndexOf(upRow);
            if (rowIndex == -1)
            {
                _logger.LogError("Failed to move test row up--couldn't find it in the list of TestRows.");
                return;
            }

            if (rowIndex == 0)
            {
                return; // Already at the top
            }
            TestRows[rowIndex] = TestRows[rowIndex - 1];
            TestRows[rowIndex - 1] = upRow;
        }

        private void TestRowDownClicked(CreateTestRowVM downRow)
        {
            int rowIndex = TestRows.IndexOf(downRow);
            if (rowIndex == -1)
            {
                _logger.LogError("Failed to move test row up--couldn't find it in the list of TestRows.");
                return;
            }

            if (rowIndex == TestRows.Count - 1)
            {
                return; //Already at the bottom
            }
            TestRows[rowIndex] = TestRows[rowIndex + 1];
            TestRows[rowIndex + 1] = downRow;
        }

        internal async void SaveListClicked()
        {
            IsSaveErrorVisible = false;
            SaveErrorString = "";
            IsSaving = true;

            List<string> errors = new List<string>();
            if (ChosenMasterList == null || ChosenMasterListName == null)
            {
                errors.Add("• You need to choose a Master List first. If you don't see any, you should go make one!");
            }
            if (TestRows.Count <= 0)
            {
                errors.Add("• A test must have at least one row.");
            }
            if (string.IsNullOrWhiteSpace(TestName))
            {
                errors.Add("• A test must have a name.");
            }

            if (errors.Any())
            {
                IsSaveErrorVisible = true;
                SaveErrorString = string.Join(Environment.NewLine, errors);
                IsSaving = false;
                StateHasChanged();
                return;
            }

            // Save the list
            TestRow[]? result = await _networkService.CreateTest(
                new CreateTestRequest(
                    Name: TestName,
                    Rows: TestRows
                        .Select(x => new CreateTestRowRequest(x.Words, x.HiddenColumnIndices.ToArray(), x.Hint))
                        .ToArray(),
                    SourceList: ChosenMasterList!.Value,
                    SourceListName: ChosenMasterListName!
                ),
                CancellationToken.None
            );

            if (result == null)
            {
                IsSaveErrorVisible = true;
                SaveErrorString = "Failed to save your new test.";
                IsSaving = false;
                StateHasChanged();
                return;
            }

            // Request succeeded
            IsSaving = false;
            NavManager.NavigateTo("dashboard/tests");
        }
    }
}

using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using VerbIt.Client.Models;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.MasterLists
{
    public partial class ViewSingleList : ComponentBase
    {
        [CascadingParameter]
        private DashboardLayout Layout { get; set; } = null!;

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        [Inject]
        private ILocalStorageService _localStorageService { get; set; } = null!;

        [Parameter]
        public string ListId { get; set; } = null!;

        // View-mode specific
        private bool _listExists = false;
        private Guid? _listId = null;

        // Shared between view and edit modes
        private string ListName { get; set; } = null!;
        private List<MasterListRow> RowList { get; set; } = null!;

        // Edit-mode specific
        private const string PrevSavedCreateMasterList = "prevSavedCreateMasterList";
        private const string ListNameDefaultClass = "masterlist-default";
        private const string ListNameErrorClass = "masterlist-error";
        private List<EditListRowVM> EditRowList { get; set; } = new List<EditListRowVM>();
        private bool IsSaving { get; set; } = false;
        private string ListNameFieldClass { get; set; } = ListNameDefaultClass;
        private bool IsEditMode { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            Layout.Title = $"Master Lists - Loading...";
            Layout.BackButtonText = "↑ Go up to Master Lists";

            if (!Guid.TryParse(ListId, out Guid listId))
            {
                return;
            }

            _listId = listId;

            MasterListRow[]? rowList = await _networkService.GetMasterList(listId, CancellationToken.None);
            if (rowList == null)
            {
                return;
            }

            _listExists = true;
            RowList = new List<MasterListRow>(rowList);
            ListName = RowList[0].ListName;
            Layout.Title = $"Master Lists - {ListName}";

            var thisUri = NavManager.ToAbsoluteUri(NavManager.Uri);
            if (QueryHelpers.ParseNullableQuery(thisUri.Query)?.TryGetValue("editMode", out var editMode) == true)
            {
                if (bool.Parse(editMode) == true)
                {
                    IsEditMode = true;
                }
            }

            if (QueryHelpers.ParseNullableQuery(thisUri.Query)?.TryGetValue("restore", out var restoreCachedList) == true)
            {
                if (bool.Parse(restoreCachedList) == true)
                {
                    await RestoreCachedList();
                }
            }
        }

        private string? _listNameBeforeEdits = null;

        internal void EditModeClicked()
        {
            IsEditMode = true;
            _listNameBeforeEdits = ListName;

            EditRowList = new List<EditListRowVM>(
                RowList.Select(
                    x =>
                        new EditListRowVM
                        {
                            Words = x.Words.Select(x => x.ToList()).ToList(),
                            RowId = x.RowId,
                            RowNum = x.RowNum
                        }
                )
            );
        }

        // --- Edit-mode functions ---
        private async Task RestoreCachedList()
        {
            if (await _localStorageService.ContainKeyAsync(PrevSavedCreateMasterList))
            {
                string savedListString = await _localStorageService.GetItemAsync<string>(PrevSavedCreateMasterList);
                await _localStorageService.RemoveItemAsync(PrevSavedCreateMasterList);
                if (!string.IsNullOrWhiteSpace(savedListString))
                {
                    EditListRowVM[] savedRows = JsonSerializer.Deserialize<EditListRowVM[]>(savedListString)!;
                    EditRowList = new List<EditListRowVM>(savedRows);
                    return;
                }
            }
        }

        internal async Task SaveChanges()
        {
            IsSaving = true;
            if (string.IsNullOrWhiteSpace(ListName))
            {
                ListNameFieldClass = ListNameErrorClass;
                IsSaving = false;
                return;
            }

            // Saved the row state just in case this request fails and the user gets booted to the login screen
            string listStateString = JsonSerializer.Serialize(EditRowList.ToArray());
            await _localStorageService.SetItemAsync(PrevSavedCreateMasterList, listStateString);

            var editRequest = new EditMasterListRequest(
                ListName,
                EditRowList
                    .Select(x => new EditMasterListRowRequest(x.RowId, x.RowNum, x.Words.Select(y => y.ToArray()).ToArray()))
                    .ToArray()
            );

            var updatedList = await _networkService.EditMasterList(_listId!.Value, editRequest, CancellationToken.None);
            if (updatedList == null)
            {
                // TODO: Display oh no failure message
                IsSaving = false;
                return;
            }

            RowList = updatedList.ToList();
            ListName = updatedList[0].ListName;

            IsSaving = false;
            IsEditMode = false;

            // Clear out the just-in-case cache
            await _localStorageService.RemoveItemAsync(PrevSavedCreateMasterList);
        }

        internal void DiscardChangesClicked()
        {
            if (_listNameBeforeEdits != null)
            {
                ListName = _listNameBeforeEdits;
                _listNameBeforeEdits = null;
            }
            IsEditMode = false;
        }

        internal void RemoveRowClicked(EditListRowVM row)
        {
            EditRowList.Remove(row);
        }

        internal void OnListNameInput()
        {
            ListNameFieldClass = ListNameDefaultClass;
        }
    }
}

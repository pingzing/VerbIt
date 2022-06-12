using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;
using VerbIt.Client.Models;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.MasterLists
{
    public partial class CreateList : ComponentBase
    {
        private const string PrevSavedMasterList = "prevSavedMasterList";
        private const string ListNameDefaultClass = "masterlist-default";
        private const string ListNameErrorClass = "masterlist-error";
        private const string UploadCsvDefaultClass = "btn-input";
        private const string UploadCsvDisabledClass = "btn-input-disabled";

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private ICsvImporterService _csvImporterService { get; set; } = null!;

        [Inject]
        private ILogger<CreateList> _logger { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        [Inject]
        private ILocalStorageService _localStorageService { get; set; } = null!;

        private bool IsSaving { get; set; } = false;
        private string ListName { get; set; } = "";
        private string ListNameFieldClass { get; set; } = ListNameDefaultClass;
        private string UploadCsvButtonClass { get; set; } = UploadCsvDefaultClass;
        private List<CreateListRowVM> RowList { get; set; } = new List<CreateListRowVM>();
        private int InitialCount { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            if (await _localStorageService.ContainKeyAsync(PrevSavedMasterList))
            {
                string savedListString = await _localStorageService.GetItemAsync<string>(PrevSavedMasterList);
                await _localStorageService.RemoveItemAsync(PrevSavedMasterList);
                if (!string.IsNullOrWhiteSpace(savedListString))
                {
                    CreateListRowVM[] savedRows = JsonSerializer.Deserialize<CreateListRowVM[]>(savedListString)!;
                    RowList = new List<CreateListRowVM>(savedRows);
                    return;
                }
            }

            var thisUri = NavManager.ToAbsoluteUri(NavManager.Uri);
            if (QueryHelpers.ParseNullableQuery(thisUri.Query)?.TryGetValue("initialCount", out var initialCount) == true)
            {
                InitialCount = int.Parse(initialCount);
            }

            for (int i = 0; i < InitialCount; i++)
            {
                RowList.Add(CreateListRowVM.GetDefaultRow());
            }
        }

        internal void AddRowClicked()
        {
            RowList.Add(CreateListRowVM.GetDefaultRow());
        }

        internal async Task LoadCsvFiles(InputFileChangeEventArgs e)
        {
            try
            {
                List<CreateListRowVM> importedList = new();
                await foreach (
                    CreateListRowVM row in _csvImporterService.ImportMasterList(e.File.OpenReadStream(), CancellationToken.None)
                )
                {
                    importedList.Add(row);
                }
                RowList = importedList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to read CSV file {e.File.Name}.");
                // TODO: Report this to the user
            }
        }

        internal void RemoveRowClicked(CreateListRowVM row)
        {
            RowList.Remove(row);
        }

        internal async Task SaveListClicked()
        {
            IsSaving = true;
            UploadCsvButtonClass = UploadCsvDisabledClass;
            if (string.IsNullOrWhiteSpace(ListName))
            {
                ListNameFieldClass = ListNameErrorClass;
                IsSaving = false;
                UploadCsvButtonClass = UploadCsvDefaultClass;
                return;
            }

            // Saved the row state just in case this request fails and the user gets booted to the login screen
            string listStateString = JsonSerializer.Serialize<CreateListRowVM[]>(RowList.ToArray());
            await _localStorageService.SetItemAsync<string>(PrevSavedMasterList, listStateString);

            var createRequest = new CreateMasterListRequest(
                ListName,
                RowList.Select(x => new CreateMasterListRowRequest(x.Words.Select(y => y.ToArray()).ToArray())).ToArray()
            );

            var createdList = await _networkService.CreateMasterList(createRequest, CancellationToken.None);
            if (createdList == null)
            {
                // TODO: Display an error. If we got a 401, we'll be redirected to /login shortly
            }
            else
            {
                // Request succeeded, no need to keep the saved state around
                await _localStorageService.RemoveItemAsync(PrevSavedMasterList);
                NavManager.NavigateTo("dashboard/masterlists");
            }

            // Save list, redirect back to "see all master lists" page
            UploadCsvButtonClass = UploadCsvDefaultClass;
            IsSaving = false;
        }

        internal void OnListNameInput()
        {
            ListNameFieldClass = ListNameDefaultClass;
        }
    }
}

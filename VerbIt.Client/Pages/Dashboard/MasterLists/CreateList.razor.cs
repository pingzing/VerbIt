using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using VerbIt.Client.Models;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.MasterLists
{
    public partial class CreateList : ComponentBase
    {
        private const string _listNameDefaultClass = "masterlist-default";
        private const string _listNameErrorClass = "masterlist-error";
        private const string _uploadCsvDefaultClass = "btn-input";
        private const string _uploadCsvDisabledClass = "btn-input-disabled";

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private ICsvImporterService _csvImporterService { get; set; } = null!;

        [Inject]
        private ILogger<CreateList> _logger { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        private bool IsSaving { get; set; } = false;
        private string ListName { get; set; } = "";
        private string ListNameFieldClass { get; set; } = _listNameDefaultClass;
        private string UploadCsvButtonClass { get; set; } = _uploadCsvDefaultClass;
        private List<CreateListRowVM> RowList { get; set; } = new List<CreateListRowVM>();
        private int InitialCount { get; set; } = 1;

        protected override void OnInitialized()
        {
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
            Console.WriteLine($"Removing row at index: {RowList.IndexOf(row)}");
            RowList.Remove(row);
        }

        internal async Task SaveListClicked()
        {
            IsSaving = true;
            UploadCsvButtonClass = _uploadCsvDisabledClass;
            if (string.IsNullOrWhiteSpace(ListName))
            {
                ListNameFieldClass = _listNameErrorClass;
                IsSaving = false;
                UploadCsvButtonClass = _uploadCsvDefaultClass;
                return;
            }

            var createRequest = new CreateMasterListRequest(
                ListName,
                RowList.Select(x => new CreateMasterListRowRequest(x.Words.Select(y => y.ToArray()).ToArray())).ToArray()
            );

            var createdList = await _networkService.CreateMasterList(createRequest, CancellationToken.None);

            NavManager.NavigateTo("dashboard/masterlists");

            // Save list, redirect back to "see all master lists" page
            UploadCsvButtonClass = _uploadCsvDefaultClass;
            IsSaving = false;
        }

        internal void OnListNameInput()
        {
            ListNameFieldClass = _listNameDefaultClass;
        }
    }
}

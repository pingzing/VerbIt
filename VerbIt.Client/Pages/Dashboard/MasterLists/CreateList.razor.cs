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
        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private ICsvImporterService _csvImporterService { get; set; } = null!;

        [Inject]
        private ILogger<CreateList> _logger { get; set; } = null!;

        private string ListName { get; set; } = "New Master List Name";
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

        internal void CellClicked(CreateListRowVM row, int rowIndex, int colIndex)
        {
            Console.WriteLine($"Cell with rownum: {rowIndex + 1} and colIndex {colIndex} clicked");
            // TODO: Replace the clicked cell with an editable text field containing the column's words in a pipe-separated list.
            // When the user leaves focus, or clicks accept, split by pipe, and save into this row.
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
    }
}

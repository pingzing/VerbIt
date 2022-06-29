using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using VerbIt.Client.Components;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.MasterLists
{
    public partial class ViewLists : ComponentBase
    {
        [CascadingParameter]
        private IModalService _modalService { get; set; } = null!;

        [CascadingParameter]
        private DashboardLayout Layout { get; set; } = null!;

        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        private List<SavedMasterList>? SavedLists { get; set; } = null;

        protected override async Task OnInitializedAsync()
        {
            Layout.Title = "Master Lists";
            Layout.BackButtonText = "↑ Go up to Dashboard";

            var getListsResult = await _networkService.GetMasterLists(CancellationToken.None);
            if (getListsResult == null)
            {
                // TODO: Show error
                return;
            }

            SavedLists = new List<SavedMasterList>(getListsResult.OrderBy(x => x.ListCreationTimestamp));
        }

        private async Task DeleteMasterListClicked(SavedMasterList list)
        {
            var modalParams = new ModalParameters();
            modalParams.Add(nameof(MessageBox.Message), $"Are you sure you want to delete master list '{list.ListName}'?");
            modalParams.Add(nameof(MessageBox.OkState), new ButtonState(true, "Yes, delete"));
            modalParams.Add(nameof(MessageBox.CancelState), new ButtonState(true, "No, cancel"));
            var result = await _modalService.Show<MessageBox>("Delete list", modalParams).Result;
            if (result.Cancelled)
            {
                return;
            }

            bool deleteResult = await _networkService.DeleteMasterList(list.ListId, CancellationToken.None);
            if (!deleteResult)
            {
                // TODO: Display sadness
                return;
            }

            SavedLists?.Remove(list);
        }
    }
}

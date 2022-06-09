using Microsoft.AspNetCore.Components;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.MasterLists
{
    public partial class ViewLists : ComponentBase
    {
        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        private List<SavedMasterList>? SavedLists { get; set; } = null;

        protected override async Task OnParametersSetAsync()
        {
            var getListsResult = await _networkService.GetMasterLists(CancellationToken.None);
            if (getListsResult == null)
            {
                // TODO: Show error
                return;
            }

            SavedLists = new List<SavedMasterList>(getListsResult.OrderBy(x => x.ListCreationTimestamp));
        }
    }
}

using Microsoft.AspNetCore.Components;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages.Dashboard.MasterLists
{
    public partial class ViewSingleList : ComponentBase
    {
        [Inject]
        private NavigationManager NavManager { get; set; } = null!;

        [Inject]
        private INetworkService _networkService { get; set; } = null!;

        [Parameter]
        public string ListId { get; set; } = null!;

        private bool _listExists = false;
        private Guid? _listId = null;

        private List<MasterListRow> RowList { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
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
        }
    }
}

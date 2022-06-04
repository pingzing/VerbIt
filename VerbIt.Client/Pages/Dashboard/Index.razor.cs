using Microsoft.AspNetCore.Components;

namespace VerbIt.Client.Pages.Dashboard;

public partial class Index : ComponentBase
{
    [Inject]
    private NavigationManager NavManager { get; set; } = null!;

    private string MasterListUri => $"{NavManager.Uri}/masterlists";
}

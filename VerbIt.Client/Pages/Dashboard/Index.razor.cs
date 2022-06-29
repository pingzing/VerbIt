using Microsoft.AspNetCore.Components;

namespace VerbIt.Client.Pages.Dashboard;

public partial class Index : ComponentBase
{
    [Inject]
    private NavigationManager NavManager { get; set; } = null!;

    [CascadingParameter]
    private DashboardLayout Layout { get; set; } = null!;

    protected override void OnInitialized()
    {
        Layout.Title = "Dashboard";
        Layout.BackButtonText = "↑ Go up to the main page";
    }
}

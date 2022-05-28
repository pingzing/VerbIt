using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages;

public partial class Login : ComponentBase
{
    [Inject]
    private INetworkService NetworkService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "originalUrl")]
    public string? EscapedOriginalUrl { get; set; }

    // Data-bound against UI input
    private LoginRequest loginRequest = new LoginRequest();

    private async Task CallLogin()
    {
        bool loginSuccess = await NetworkService.Login(loginRequest, CancellationToken.None);
        if (!loginSuccess)
        {
            // TODO: Display error
            return;
        }

        if (EscapedOriginalUrl != null)
        {
            NavigationManager.NavigateTo(Uri.UnescapeDataString(EscapedOriginalUrl));
        }
        else
        {
            NavigationManager.NavigateTo("/dashboard");
        }
    }
}

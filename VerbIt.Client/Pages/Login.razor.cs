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
    private string? _username;
    private string? _password;

    private async Task CallLogin()
    {
        if (_username == null || _password == null)
        {
            // Display error
            return;
        }

        bool loginSuccess = await NetworkService.Login(new LoginRequest(_username, _password), CancellationToken.None);
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

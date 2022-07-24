using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using VerbIt.Client.Services;
using VerbIt.DataModels;

namespace VerbIt.Client.Pages;

public partial class Login : ComponentBase
{
    private const string HiddenClass = "hidden";
    private const string VisibleClass = "";

    [Inject]
    private INetworkService NetworkService { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "originalUrl")]
    public string? EscapedOriginalUrl { get; set; }

    private bool IsErrorLabelVisible { get; set; } = false;
    private string ErrorLabelHidden
    {
        get => $"{(IsErrorLabelVisible ? VisibleClass : HiddenClass)}";
    }
    private string? ErrorLabel { get; set; } = null;

    private bool IsWaitingForNetwork { get; set; } = false;

    // Data-bound against UI input
    private string? _username;
    private string? _password;

    private async Task CallLogin()
    {
        IsErrorLabelVisible = false;
        IsWaitingForNetwork = true;
        if (_username == null || _password == null)
        {
            IsErrorLabelVisible = true;
            ErrorLabel = "Both username and password must have a value";
            IsWaitingForNetwork = false;
            return;
        }

        bool loginSuccess = await NetworkService.Login(new LoginRequest(_username, _password), CancellationToken.None);
        if (!loginSuccess)
        {
            IsErrorLabelVisible = true;
            ErrorLabel = "Server failed to log you in. Check your username and password.";
            IsWaitingForNetwork = false;
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

        IsWaitingForNetwork = false;
    }
}

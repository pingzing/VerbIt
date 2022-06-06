using Microsoft.AspNetCore.Components;
using VerbIt.Client.Services;

namespace VerbIt.Client.Authentication;

public class RedirectingAuthHandler : DelegatingHandler
{
    public RedirectingAuthHandler() { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // TODO: Fire a logout event somehow
        }

        return response;
    }
}

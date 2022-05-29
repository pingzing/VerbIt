using Azure.Core;
using Azure.Core.Pipeline;

namespace VerbIt.Backend.Repositories;

/// <summary>
/// A custom HttpPipelinePolicy for customizing the headers sent via the Azure Table Client.
/// </summary>
public class PreferReturnContentPolicy : HttpPipelineSynchronousPolicy
{
    public override void OnSendingRequest(HttpMessage message) =>
        message.Request.Headers.Add("Prefer", "return-content");
}

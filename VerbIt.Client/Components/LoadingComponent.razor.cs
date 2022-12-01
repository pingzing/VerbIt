using Microsoft.AspNetCore.Components;

namespace VerbIt.Client.Components;

public partial class LoadingComponent : ComponentBase
{
    [Parameter]
    public LoadingState State { get; set; }

    [Parameter]
    public RenderFragment? NotStartedContent { get; set; }

    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// Used to render the "success" state.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = null!;

    [Parameter]
    public RenderFragment FailureContent { get; set; } = null!;

    /// <summary>
    /// Used only if no <see cref="FailureContent"/> is set.
    /// If neither this nor <see cref="FailureContent"/> is set, the
    /// text 'Loading failure.' is displayed.
    /// </summary>
    [Parameter]
    public string? FailureText { get; set; }
}

public enum LoadingState
{
    NotStarted,
    Loading,
    Success,
    Failure,
}

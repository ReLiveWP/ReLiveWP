// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ReLiveWP.Identity.LiveID;

public class LiveIDAuthEvents
{
    /// <summary>
    /// Invoked if authentication fails during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public Func<LiveIDAuthenticationFailedContext, Task> OnAuthenticationFailed { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked if Authorization fails and results in a Forbidden response.
    /// </summary>
    public Func<LiveIDForbiddenContext, Task> OnForbidden { get; set; } = context => Task.CompletedTask;
    /// <summary>
    /// Invoked before a challenge is sent back to the caller.
    /// </summary>
    public Func<LiveIDChallengeContext, Task> OnChallenge { get; set; } = context => Task.CompletedTask;

    /// <summary>
    /// Invoked if exceptions are thrown during request processing. The exceptions will be re-thrown after this event unless suppressed.
    /// </summary>
    public virtual Task AuthenticationFailed(LiveIDAuthenticationFailedContext context) => OnAuthenticationFailed(context);

    /// <summary>
    /// Invoked if Authorization fails and results in a Forbidden response
    /// </summary>
    public virtual Task Forbidden(LiveIDForbiddenContext context) => OnForbidden(context);

    /// <summary>
    /// Invoked before a challenge is sent back to the caller.
    /// </summary>
    public virtual Task Challenge(LiveIDChallengeContext context) => OnChallenge(context);

}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;

namespace ReLiveWP.Identity.LiveID;

public record LiveIDAuthenticationFailedContext(HttpContext Context, AuthenticationScheme Scheme, LiveIDAuthOptions Options)
{
    public Exception? Exception { get; set; }
}

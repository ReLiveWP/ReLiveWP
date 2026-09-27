
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;

namespace ReLiveWP.Identity.LiveID;

public class LiveIDAuthOptions : AuthenticationSchemeOptions
{
    public string? HeaderNameOverride { get; set; }
    public IReadOnlyList<string> ValidServiceTargets { internal get; set; } = [];
    public bool AcceptBasicAuth { get; set; }
    public string? CookieNameFallback { get; set; }
}

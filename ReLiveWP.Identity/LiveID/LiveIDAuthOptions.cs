
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;

namespace ReLiveWP.Identity.LiveID;

public class LiveIDAuthOptions : AuthenticationSchemeOptions
{
    public string? HeaderNameOverride { get; set; }
    public IReadOnlyList<string> ValidServiceTargets { internal get; set; } = [];

    // When true, the handler also accepts HTTP Basic auth where the password field
    // is an access token: Authorization: Basic base64(username:access_token).
    // The token is verified identically to a Bearer token; the username is ignored
    // for verification but may be present in the returned claims.
    public bool AcceptBasicAuth { get; set; }

    // Realm sent in the WWW-Authenticate challenge when AcceptBasicAuth is true.
    public string BasicAuthRealm { get; set; } = "Exchange";
}

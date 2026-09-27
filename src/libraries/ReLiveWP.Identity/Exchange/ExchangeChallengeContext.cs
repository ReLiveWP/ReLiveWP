
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace ReLiveWP.Identity.Exchange;

public record ExchangeChallengeContext(HttpContext Context, AuthenticationScheme Scheme, ExchangeAuthOptions Options, AuthenticationProperties Properties)
{
    public Exception? AuthenticateFailure { get; set; }
    public bool Handled { get; set; } = false;
}

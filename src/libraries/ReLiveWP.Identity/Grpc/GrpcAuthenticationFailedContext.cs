
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace ReLiveWP.Identity.Grpc;

public record GrpcAuthenticationFailedContext(HttpContext Context, AuthenticationScheme Scheme, GrpcAuthOptions Options)
{
    public Exception? Exception { get; set; }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Identity;

public static class HttpHeaders
{
    public const string DPoP = "DPoP";
    public const string DPoPNonce = "DPoP-Nonce";
}

/// <summary>
/// Extensions for HTTP request/response messages
/// </summary>
public static class DPoPExtensions
{
    /// <summary>
    /// Sets the DPoP nonce request header if nonce is not null. 
    /// </summary>
    public static void SetDPoPProofToken(this HttpRequestMessage request, string? proofToken)
    {
        // remove any old headers
        request.Headers.Remove(HttpHeaders.DPoP);
        // set new header
        request.Headers.Add(HttpHeaders.DPoP, proofToken);
    }

    /// <summary>
    /// Reads the DPoP nonce header from the response
    /// </summary>
    public static string? GetDPoPNonce(this HttpResponseMessage response) =>
        response.Headers.TryGetValues(HttpHeaders.DPoPNonce, out var values)
            ? values.FirstOrDefault()
            : null;

    /// <summary>
    /// Returns the URL without any query params
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string GetDPoPUrl(this HttpRequestMessage request) =>
        request.RequestUri!.Scheme + "://" + request.RequestUri!.Authority + request.RequestUri!.LocalPath;
}


/// <summary>
/// Message handler to create and send DPoP proof tokens.
/// </summary>
internal class ProofTokenMessageHandler : DelegatingHandler
{
    private string? _nonce;
    private string connectionId;
    private string tokenAuth;
    private ConnectedServices.ConnectedServicesClient authClient;

    /// <summary>
    /// Constructor
    /// </summary>
    public ProofTokenMessageHandler(ConnectedServices.ConnectedServicesClient authClient, string connectionId,  string tokenAuth, HttpMessageHandler innerHandler)
    {
        this.authClient = authClient;
        this.connectionId = connectionId;
        this.tokenAuth = tokenAuth;
        InnerHandler = innerHandler ?? throw new ArgumentNullException(nameof(innerHandler));
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await CreateProofToken(request);

        var response = await base.SendAsync(request, cancellationToken);

        var dPoPNonce = response.GetDPoPNonce();

        if (dPoPNonce != _nonce)
        {
            // nonce is different, so hold onto it
            _nonce = dPoPNonce;

            // failure and nonce was different so we retry
            if (!response.IsSuccessStatusCode)
            {
                response.Dispose();

                await CreateProofToken(request);

                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        return response;
    }

    private async Task CreateProofToken(HttpRequestMessage request)
    {
        var proofRequest = new DPoPProofTokenRequest
        {
            ConnectionId = this.connectionId,

            Method = request.Method.ToString(),
            Url = request.GetDPoPUrl(),
            Nonce = _nonce
        };

        if (request.Headers.Authorization != null && "DPoP".Equals(request.Headers.Authorization.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            proofRequest.AccessToken = request.Headers.Authorization.Parameter;
        }

        var metadata = new Metadata() { { "Authorization", tokenAuth } };
        var proof = await authClient.GetDPoPProofTokenAsync(proofRequest, metadata);

        request.SetDPoPProofToken(proof.ProofToken);
    }
}

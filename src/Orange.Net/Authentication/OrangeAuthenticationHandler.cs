using System.Net;
using System.Net.Http.Headers;

namespace Orange.Net.Authentication;

/// <summary>
/// Adds the Orange bearer token to outgoing requests. If the gateway answers 401 (for example
/// code 42, expired credentials), the token is invalidated and the request is retried once.
/// </summary>
public sealed class OrangeAuthenticationHandler : DelegatingHandler
{
    private readonly IOrangeTokenProvider _tokenProvider;

    /// <summary>Creates the handler.</summary>
    public OrangeAuthenticationHandler(IOrangeTokenProvider tokenProvider)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        _tokenProvider = tokenProvider;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();
        _tokenProvider.Invalidate(token);
        token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

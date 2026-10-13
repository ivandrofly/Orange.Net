using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Orange.Net.Internal;

namespace Orange.Net.Authentication;

/// <summary>
/// Requests tokens from the Orange OAuth v3 endpoint with the client-credentials grant and caches them
/// until shortly before expiry. Thread-safe: concurrent callers share a single token request.
/// </summary>
public sealed class OrangeTokenProvider : IOrangeTokenProvider, IDisposable
{
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly OrangeClientOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private OrangeAccessToken? _token;

    /// <summary>Creates a token provider that sends token requests with <paramref name="httpClient"/>.</summary>
    /// <param name="httpClient">An HTTP client <b>without</b> the Orange authentication handler.</param>
    /// <param name="options">Client credentials and endpoints.</param>
    /// <param name="timeProvider">Clock used for expiry checks; defaults to <see cref="TimeProvider.System"/>.</param>
    public OrangeTokenProvider(HttpClient httpClient, OrangeClientOptions options, TimeProvider? timeProvider = null)
        : this(() => httpClient, options, timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
    }

    internal OrangeTokenProvider(Func<HttpClient> httpClientFactory, OrangeClientOptions options, TimeProvider? timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _httpClientFactory = httpClientFactory;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>The currently cached token, if any.</summary>
    public OrangeAccessToken? CurrentToken => Volatile.Read(ref _token);

    /// <inheritdoc />
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = Volatile.Read(ref _token);
        if (IsUsable(token))
            return token!.AccessToken;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            token = _token;
            if (IsUsable(token))
                return token!.AccessToken;

            token = await RequestTokenAsync(cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref _token, token);
            return token.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void Invalidate(string accessToken)
    {
        var current = Volatile.Read(ref _token);
        if (current is not null && current.AccessToken == accessToken)
            Interlocked.CompareExchange(ref _token, null, current);
    }

    /// <inheritdoc />
    public void Dispose() => _lock.Dispose();

    private bool IsUsable(OrangeAccessToken? token) =>
        token is not null && _timeProvider.GetUtcNow() < token.ExpiresAt - _options.TokenRefreshSkew;

    private async Task<OrangeAccessToken> RequestTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent([new("grant_type", "client_credentials")]),
        };
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var issuedAt = _timeProvider.GetUtcNow();
        using var response = await _httpClientFactory().SendAsync(request, cancellationToken).ConfigureAwait(false);
        await OrangeErrorParser.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var body = await response.Content.ReadFromJsonAsync(OrangeJsonContext.Default.TokenResponse, cancellationToken).ConfigureAwait(false);
        if (body is null || string.IsNullOrEmpty(body.AccessToken))
            throw new OrangeApiException(response.StatusCode, null, "Token response did not contain an access_token.", null, null);

        var lifetime = TimeSpan.FromSeconds(body.ExpiresIn is > 0 ? body.ExpiresIn.Value : 3600);
        return new OrangeAccessToken(body.AccessToken, body.TokenType ?? "Bearer", body.Scope, issuedAt + lifetime);
    }
}

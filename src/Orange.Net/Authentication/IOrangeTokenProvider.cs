namespace Orange.Net.Authentication;

/// <summary>Supplies cached OAuth 2.0 access tokens for Orange API calls.</summary>
public interface IOrangeTokenProvider
{
    /// <summary>
    /// Returns a valid access token, requesting a new one only when the cached token is missing or about to expire.
    /// </summary>
    ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards the cached token if it is still <paramref name="accessToken"/>, forcing the next call to request a new one.
    /// </summary>
    void Invalidate(string accessToken);
}

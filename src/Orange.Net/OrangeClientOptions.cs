namespace Orange.Net;

/// <summary>
/// Credentials and endpoints used by every Orange Developer API client.
/// Client ID and secret are shown under "MyApps" on https://developer.orange.com.
/// </summary>
public sealed class OrangeClientOptions
{
    /// <summary>Default configuration section name used by the <c>IConfiguration</c> overloads.</summary>
    public const string SectionName = "Orange";

    /// <summary>The application's client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The application's client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Root of the Orange API gateway. Defaults to <c>https://api.orange.com/</c>.</summary>
    public Uri BaseAddress { get; set; } = new("https://api.orange.com/");

    /// <summary>Token endpoint, relative to <see cref="BaseAddress"/>. Defaults to the OAuth v3 endpoint.</summary>
    public string TokenPath { get; set; } = "oauth/v3/token";

    /// <summary>
    /// How long before the token's actual expiry a new one is requested. Defaults to 60 seconds.
    /// </summary>
    public TimeSpan TokenRefreshSkew { get; set; } = TimeSpan.FromSeconds(60);

    internal Uri NormalizedBaseAddress =>
        BaseAddress.AbsoluteUri.EndsWith('/') ? BaseAddress : new Uri(BaseAddress.AbsoluteUri + "/");

    internal Uri TokenEndpoint => new(NormalizedBaseAddress, TokenPath);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException($"{nameof(OrangeClientOptions)}.{nameof(ClientId)} is required.");
        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new InvalidOperationException($"{nameof(OrangeClientOptions)}.{nameof(ClientSecret)} is required.");
        if (BaseAddress is null || !BaseAddress.IsAbsoluteUri)
            throw new InvalidOperationException($"{nameof(OrangeClientOptions)}.{nameof(BaseAddress)} must be an absolute URI.");
    }
}

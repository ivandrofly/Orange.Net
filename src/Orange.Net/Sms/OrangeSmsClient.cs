using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Orange.Net.Authentication;
using Orange.Net.Internal;

namespace Orange.Net.Sms;

/// <summary>
/// Client for the Orange SMS Middle East &amp; Africa API.
/// Register it with <c>services.AddOrangeSms(...)</c>, or build a standalone instance with <see cref="Create"/>.
/// </summary>
public sealed class OrangeSmsClient : IOrangeSmsClient
{
    private const string MessagingPath = "smsmessaging/v1/";
    private const string AdminPath = "sms/admin/v1/";

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a client over <paramref name="httpClient"/>, which must have <see cref="HttpClient.BaseAddress"/> set to the
    /// Orange gateway and an <see cref="OrangeAuthenticationHandler"/> in its pipeline.
    /// </summary>
    public OrangeSmsClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a standalone client with its own HTTP pipeline and token cache. Create it once and reuse it.
    /// </summary>
    /// <param name="options">Credentials and endpoints.</param>
    /// <param name="innerHandler">Optional transport handler (for proxies or tests). Defaults to a pooled <see cref="SocketsHttpHandler"/>.</param>
    public static OrangeSmsClient Create(OrangeClientOptions options, HttpMessageHandler? innerHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        innerHandler ??= new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
        var tokenProvider = new OrangeTokenProvider(new HttpClient(innerHandler, disposeHandler: false), options);
        var authHandler = new OrangeAuthenticationHandler(tokenProvider) { InnerHandler = innerHandler };
        return new OrangeSmsClient(new HttpClient(authHandler) { BaseAddress = options.NormalizedBaseAddress });
    }

    /// <inheritdoc />
    public async Task<SendSmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var address = PhoneNumber.ToTelUri(message.To, nameof(message.To));
        var senderAddress = PhoneNumber.ToTelUri(message.SenderAddress, nameof(message.SenderAddress));
        PhoneNumber.ValidateSenderName(message.SenderName, nameof(message.SenderName));
        ArgumentException.ThrowIfNullOrEmpty(message.Text, nameof(message.Text));

        var path = $"{MessagingPath}outbound/{Uri.EscapeDataString(senderAddress)}/requests";
        if (message.OnNet)
            path += "?resource_type_parameter_management=SMS_OCB2";

        var body = new OutboundSmsMessageEnvelope
        {
            OutboundSmsMessageRequest = new OutboundSmsMessageRequest
            {
                Address = address,
                SenderAddress = senderAddress,
                SenderName = message.SenderName,
                OutboundSmsTextMessage = new OutboundSmsTextMessage { Message = message.Text },
            },
        };

        using var response = await _httpClient
            .PostAsJsonAsync(path, body, OrangeJsonContext.Default.OutboundSmsMessageEnvelope, cancellationToken)
            .ConfigureAwait(false);
        await OrangeErrorParser.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        OutboundSmsMessageRequest? echoed = null;
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(responseBody))
            echoed = JsonSerializer.Deserialize(responseBody, OrangeJsonContext.Default.OutboundSmsMessageEnvelope)?.OutboundSmsMessageRequest;

        var resourceUrl = response.Headers.Location?.OriginalString ?? echoed?.ResourceUrl;
        var resourceId = ExtractResourceId(resourceUrl)
            ?? throw new OrangeApiException(response.StatusCode, null, "Response did not include a resource ID (Location header or resourceURL).", null, null);

        return new SendSmsResult
        {
            ResourceId = resourceId,
            ResourceUrl = resourceUrl,
            Address = address,
            SenderAddress = senderAddress,
            SenderName = message.SenderName,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SmsContract>> GetContractsAsync(string? country = null, CancellationToken cancellationToken = default) =>
        await GetAsync(AdminPath + "contracts" + Query(("country", country)), OrangeJsonContext.Default.ListSmsContract, cancellationToken)
            .ConfigureAwait(false) ?? [];

    /// <inheritdoc />
    public async Task<SmsStatistics> GetStatisticsAsync(string? country = null, string? applicationId = null, CancellationToken cancellationToken = default)
    {
        var envelope = await GetAsync(
                AdminPath + "statistics" + Query(("country", country), ("appid", applicationId)),
                OrangeJsonContext.Default.SmsStatisticsEnvelope,
                cancellationToken)
            .ConfigureAwait(false);
        return envelope?.PartnerStatistics ?? new SmsStatistics();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SmsPurchaseOrder>> GetPurchaseOrdersAsync(string? country = null, CancellationToken cancellationToken = default) =>
        await GetAsync(AdminPath + "purchaseorders" + Query(("country", country)), OrangeJsonContext.Default.ListSmsPurchaseOrder, cancellationToken)
            .ConfigureAwait(false) ?? [];

    internal static string? ExtractResourceId(string? resourceUrl)
    {
        if (string.IsNullOrWhiteSpace(resourceUrl))
            return null;

        var trimmed = resourceUrl.TrimEnd('/');
        var id = trimmed[(trimmed.LastIndexOf('/') + 1)..];
        return id.Length == 0 ? null : id;
    }

    private async Task<T?> GetAsync<T>(string path, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
        await OrangeErrorParser.EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(typeInfo, cancellationToken).ConfigureAwait(false);
    }

    private static string Query(params ReadOnlySpan<(string Name, string? Value)> parameters)
    {
        var query = string.Empty;
        foreach (var (name, value) in parameters)
        {
            if (!string.IsNullOrWhiteSpace(value))
                query += $"{(query.Length == 0 ? '?' : '&')}{name}={Uri.EscapeDataString(value)}";
        }
        return query;
    }
}

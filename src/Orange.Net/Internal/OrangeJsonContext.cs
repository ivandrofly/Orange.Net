using System.Text.Json;
using System.Text.Json.Serialization;
using Orange.Net.Sms;

namespace Orange.Net.Internal;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(OutboundSmsMessageEnvelope))]
[JsonSerializable(typeof(DeliveryInfoNotificationEnvelope))]
[JsonSerializable(typeof(List<SmsContract>))]
[JsonSerializable(typeof(SmsStatisticsEnvelope))]
[JsonSerializable(typeof(List<SmsPurchaseOrder>))]
internal sealed partial class OrangeJsonContext : JsonSerializerContext;

internal sealed class TokenResponse
{
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    // Orange documents this both as a number and as a string ("3600"); AllowReadingFromString covers both.
    [JsonPropertyName("expires_in")]
    public long? ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

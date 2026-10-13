using System.Text.Json.Serialization;

namespace Orange.Net.Sms;

// Wire format of the send request and response (GSMA OneAPI). Property names are mixed-case, so they are spelled out.

internal sealed class OutboundSmsMessageEnvelope
{
    [JsonPropertyName("outboundSMSMessageRequest")]
    public OutboundSmsMessageRequest? OutboundSmsMessageRequest { get; set; }
}

internal sealed class OutboundSmsMessageRequest
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("senderAddress")]
    public string? SenderAddress { get; set; }

    [JsonPropertyName("senderName")]
    public string? SenderName { get; set; }

    [JsonPropertyName("outboundSMSTextMessage")]
    public OutboundSmsTextMessage? OutboundSmsTextMessage { get; set; }

    [JsonPropertyName("resourceURL")]
    public string? ResourceUrl { get; set; }
}

internal sealed class OutboundSmsTextMessage
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

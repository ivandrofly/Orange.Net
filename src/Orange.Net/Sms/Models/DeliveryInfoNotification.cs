using System.Text.Json;
using System.Text.Json.Serialization;
using Orange.Net.Internal;

namespace Orange.Net.Sms;

/// <summary>
/// A delivery receipt that Orange POSTs to your registered HTTPS endpoint (port 443, CA-signed certificate)
/// within 24 hours of sending. Your endpoint must answer <c>200 OK</c>.
/// </summary>
public sealed class DeliveryInfoNotification
{
    /// <summary>The <see cref="SendSmsResult.ResourceId"/> of the original send request.</summary>
    public string? CallbackData { get; set; }

    /// <summary>Recipient address and delivery status.</summary>
    public DeliveryInfo? DeliveryInfo { get; set; }

    /// <summary>Parses the JSON body Orange sends, including its <c>deliveryInfoNotification</c> wrapper.</summary>
    public static DeliveryInfoNotification Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Unwrap(JsonSerializer.Deserialize(json, OrangeJsonContext.Default.DeliveryInfoNotificationEnvelope));
    }

    /// <summary>Parses the JSON body Orange sends, including its <c>deliveryInfoNotification</c> wrapper.</summary>
    public static async Task<DeliveryInfoNotification> ParseAsync(Stream json, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(json);
        var envelope = await JsonSerializer
            .DeserializeAsync(json, OrangeJsonContext.Default.DeliveryInfoNotificationEnvelope, cancellationToken)
            .ConfigureAwait(false);
        return Unwrap(envelope);
    }

    private static DeliveryInfoNotification Unwrap(DeliveryInfoNotificationEnvelope? envelope) =>
        envelope?.DeliveryInfoNotification
        ?? throw new JsonException("The payload does not contain a 'deliveryInfoNotification' object.");
}

/// <summary>Recipient and status part of a <see cref="DeliveryInfoNotification"/>.</summary>
public sealed class DeliveryInfo
{
    /// <summary>Recipient address, in <c>tel:+…</c> form.</summary>
    public string? Address { get; set; }

    /// <summary>The status string exactly as Orange sent it.</summary>
    [JsonPropertyName("deliveryStatus")]
    public string? DeliveryStatusRaw { get; set; }

    /// <summary>The parsed status, or <see cref="DeliveryStatus.Unknown"/> for unrecognised values.</summary>
    [JsonIgnore]
    public DeliveryStatus Status =>
        Enum.TryParse<DeliveryStatus>(DeliveryStatusRaw, ignoreCase: true, out var status) && Enum.IsDefined(status)
            ? status
            : DeliveryStatus.Unknown;
}

/// <summary>SMS delivery status reported in delivery receipts.</summary>
public enum DeliveryStatus
{
    /// <summary>A status this library does not recognise; see <see cref="DeliveryInfo.DeliveryStatusRaw"/>.</summary>
    Unknown = 0,

    /// <summary>Delivered to the network.</summary>
    DeliveredToNetwork,

    /// <summary>Delivery status unknown, for example because the message was handed to another network.</summary>
    DeliveryUncertain,

    /// <summary>Delivery failed; the message expired before it could be delivered.</summary>
    DeliveryImpossible,

    /// <summary>The message is still queued.</summary>
    MessageWaiting,

    /// <summary>Received by the handset.</summary>
    DeliveredToTerminal,
}

internal sealed class DeliveryInfoNotificationEnvelope
{
    public DeliveryInfoNotification? DeliveryInfoNotification { get; set; }
}

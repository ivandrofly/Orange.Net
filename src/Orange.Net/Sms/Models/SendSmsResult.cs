namespace Orange.Net.Sms;

/// <summary>Result of a successful send request.</summary>
public sealed record SendSmsResult
{
    /// <summary>
    /// ID of the send request. Orange echoes it back as <see cref="DeliveryInfoNotification.CallbackData"/> in delivery receipts.
    /// </summary>
    public required string ResourceId { get; init; }

    /// <summary>Full resource URL returned by Orange, when present.</summary>
    public string? ResourceUrl { get; init; }

    /// <summary>Recipient address as sent, in <c>tel:+…</c> form.</summary>
    public required string Address { get; init; }

    /// <summary>Sender address as sent, in <c>tel:+…</c> form.</summary>
    public required string SenderAddress { get; init; }

    /// <summary>Sender name as sent, if any.</summary>
    public string? SenderName { get; init; }
}

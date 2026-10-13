namespace Orange.Net.Sms;

/// <summary>An SMS to send through the Orange SMS Middle East &amp; Africa API.</summary>
public sealed record SmsMessage
{
    /// <summary>
    /// Recipient number in international format, for example <c>+2250701020304</c>, <c>2250701020304</c> or <c>tel:+2250701020304</c>.
    /// </summary>
    public required string To { get; init; }

    /// <summary>Message text.</summary>
    public required string Text { get; init; }

    /// <summary>
    /// Your contract's sender address: the country code followed by <c>0000</c>, for example <c>2250000</c> for Côte d'Ivoire.
    /// See <see cref="OrangeSmsCountries"/>.
    /// </summary>
    public required string SenderAddress { get; init; }

    /// <summary>
    /// Optional sender name shown on the handset (max 11 letters, digits or spaces). It must be whitelisted by Orange first.
    /// </summary>
    public string? SenderName { get; init; }

    /// <summary>
    /// Use the on-net offer (Orange subscribers only), which adds <c>resource_type_parameter_management=SMS_OCB2</c> to the request.
    /// </summary>
    public bool OnNet { get; init; }
}

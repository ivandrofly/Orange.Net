namespace Orange.Net.Sms;

/// <summary>An SMS contract (bundle) for one country, with its remaining balance.</summary>
public sealed class SmsContract
{
    /// <summary>Contract ID.</summary>
    public string? Id { get; set; }

    /// <summary><c>SELFSERVICE</c> or <c>BROKER</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Developer ID.</summary>
    public string? DeveloperId { get; set; }

    /// <summary>Application ID.</summary>
    public string? ApplicationId { get; set; }

    /// <summary>ISO 3166-1 alpha-3 country code, for example <c>CIV</c>.</summary>
    public string? Country { get; set; }

    /// <summary>Offer name, for example <c>SMS_OCB</c>.</summary>
    public string? OfferName { get; set; }

    /// <summary>SMS units remaining.</summary>
    public long AvailableUnits { get; set; }

    /// <summary>SMS units requested.</summary>
    public long RequestedUnits { get; set; }

    /// <summary><c>ACTIVE</c> or <c>EXPIRED</c>.</summary>
    public string? Status { get; set; }

    /// <summary>When the contract expires.</summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>When the contract was created.</summary>
    public DateTimeOffset? CreationDate { get; set; }

    /// <summary>When the contract was last updated.</summary>
    public DateTimeOffset? LastUpdateDate { get; set; }

    /// <summary>True when <see cref="Status"/> is <c>ACTIVE</c>.</summary>
    public bool IsActive => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
}

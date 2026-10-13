namespace Orange.Net.Sms;

/// <summary>A bundle purchase from the SMS purchase history.</summary>
public sealed class SmsPurchaseOrder
{
    /// <summary>Purchase order ID.</summary>
    public string? Id { get; set; }

    /// <summary>Developer ID.</summary>
    public string? DeveloperId { get; set; }

    /// <summary>ID of the contract the bundle was added to.</summary>
    public string? ContractId { get; set; }

    /// <summary>ISO 3166-1 alpha-3 country code.</summary>
    public string? Country { get; set; }

    /// <summary>Offer name, for example <c>SMS_OCB</c>.</summary>
    public string? OfferName { get; set; }

    /// <summary>Bundle ID.</summary>
    public string? BundleId { get; set; }

    /// <summary>Bundle description.</summary>
    public string? BundleDescription { get; set; }

    /// <summary>Price paid.</summary>
    public decimal? Price { get; set; }

    /// <summary>Currency of <see cref="Price"/>.</summary>
    public string? Currency { get; set; }

    /// <summary>When the bundle was purchased.</summary>
    public DateTimeOffset? PurchaseDate { get; set; }

    /// <summary>Payment mode.</summary>
    public string? PaymentMode { get; set; }

    /// <summary>Payment provider's order ID.</summary>
    public string? PaymentProviderOrderId { get; set; }

    /// <summary>Payer ID.</summary>
    public string? PayerId { get; set; }

    /// <summary>Purchase type, for example <c>BUNDLE_PURCHASE</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Units available before the purchase.</summary>
    public long? OldAvailableUnits { get; set; }

    /// <summary>Units available after the purchase.</summary>
    public long? NewAvailableUnits { get; set; }

    /// <summary>Contract expiry before the purchase.</summary>
    public DateTimeOffset? OldExpirationDate { get; set; }

    /// <summary>Contract expiry after the purchase.</summary>
    public DateTimeOffset? NewExpirationDate { get; set; }
}

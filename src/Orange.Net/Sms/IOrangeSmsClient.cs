namespace Orange.Net.Sms;

/// <summary>Client for the Orange SMS Middle East &amp; Africa API (v1 messaging, v1 admin).</summary>
public interface IOrangeSmsClient
{
    /// <summary>Sends an SMS.</summary>
    /// <exception cref="ArgumentException">A number or the sender name is invalid.</exception>
    /// <exception cref="OrangeApiException">Orange rejected the request.</exception>
    Task<SendSmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default);

    /// <summary>Lists SMS contracts and their remaining balance, optionally for one country (ISO 3166-1 alpha-3).</summary>
    Task<IReadOnlyList<SmsContract>> GetContractsAsync(string? country = null, CancellationToken cancellationToken = default);

    /// <summary>Returns usage statistics, optionally filtered by country and application ID.</summary>
    Task<SmsStatistics> GetStatisticsAsync(string? country = null, string? applicationId = null, CancellationToken cancellationToken = default);

    /// <summary>Lists bundle purchases, optionally for one country (ISO 3166-1 alpha-3).</summary>
    Task<IReadOnlyList<SmsPurchaseOrder>> GetPurchaseOrdersAsync(string? country = null, CancellationToken cancellationToken = default);
}

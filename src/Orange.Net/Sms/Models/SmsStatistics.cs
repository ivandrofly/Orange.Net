using System.Text.Json.Serialization;

namespace Orange.Net.Sms;

/// <summary>SMS usage statistics for a developer account.</summary>
public sealed class SmsStatistics
{
    /// <summary>Developer ID.</summary>
    public string? DeveloperId { get; set; }

    /// <summary>Statistics per service (offer), for example <c>SMS_OCB</c>.</summary>
    public List<SmsServiceStatistics> Statistics { get; set; } = [];
}

/// <summary>Usage for one service (offer).</summary>
public sealed class SmsServiceStatistics
{
    /// <summary>Service (offer) name, for example <c>SMS_OCB</c>.</summary>
    public string? Service { get; set; }

    /// <summary>Usage per country.</summary>
    [JsonPropertyName("serviceStatistics")]
    public List<SmsCountryStatistics> Countries { get; set; } = [];
}

/// <summary>Usage for one country.</summary>
public sealed class SmsCountryStatistics
{
    /// <summary>ISO 3166-1 alpha-3 country code.</summary>
    public string? Country { get; set; }

    /// <summary>Usage per application.</summary>
    [JsonPropertyName("countryStatistics")]
    public List<SmsApplicationStatistics> Applications { get; set; } = [];
}

/// <summary>Usage for one application.</summary>
public sealed class SmsApplicationStatistics
{
    /// <summary>Application ID.</summary>
    [JsonPropertyName("appid")]
    public string? ApplicationId { get; set; }

    /// <summary>Number of SMS sent.</summary>
    public long Usage { get; set; }

    /// <summary>Number of enforcements (rejected requests).</summary>
    public long NbEnforcements { get; set; }
}

internal sealed class SmsStatisticsEnvelope
{
    public SmsStatistics? PartnerStatistics { get; set; }
}

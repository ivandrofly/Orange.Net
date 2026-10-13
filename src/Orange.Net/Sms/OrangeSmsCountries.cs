using System.Collections.Frozen;

namespace Orange.Net.Sms;

/// <summary>
/// Countries supported by the Orange SMS Middle East &amp; Africa API and their sender addresses.
/// </summary>
public static class OrangeSmsCountries
{
    /// <summary>Botswana.</summary>
    public const string Botswana = "BWA";
    /// <summary>Burkina Faso.</summary>
    public const string BurkinaFaso = "BFA";
    /// <summary>Cameroon.</summary>
    public const string Cameroon = "CMR";
    /// <summary>Côte d'Ivoire.</summary>
    public const string CoteDIvoire = "CIV";
    /// <summary>Guinea (Conakry).</summary>
    public const string Guinea = "GIN";
    /// <summary>Guinea-Bissau.</summary>
    public const string GuineaBissau = "GNB";
    /// <summary>Democratic Republic of the Congo.</summary>
    public const string DrCongo = "COD";
    /// <summary>Jordan.</summary>
    public const string Jordan = "JOR";
    /// <summary>Liberia.</summary>
    public const string Liberia = "LBR";
    /// <summary>Mali.</summary>
    public const string Mali = "MLI";
    /// <summary>Madagascar.</summary>
    public const string Madagascar = "MDG";
    /// <summary>Senegal.</summary>
    public const string Senegal = "SEN";
    /// <summary>Tunisia.</summary>
    public const string Tunisia = "TUN";

    private static readonly FrozenDictionary<string, string> SenderAddresses = new Dictionary<string, string>
    {
        [Botswana] = "2670000",
        [BurkinaFaso] = "2260000",
        [Cameroon] = "2370000",
        [CoteDIvoire] = "2250000",
        [Guinea] = "2240000",
        [GuineaBissau] = "2450000",
        [DrCongo] = "2430000",
        [Jordan] = "9620000",
        [Liberia] = "2310000",
        [Mali] = "2230000",
        [Madagascar] = "2610000",
        [Senegal] = "2210000",
        [Tunisia] = "2160000",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>ISO 3166-1 alpha-3 codes of all supported countries.</summary>
    public static IReadOnlyCollection<string> All => SenderAddresses.Keys;

    /// <summary>
    /// Returns the sender address for a country, for example <c>2250000</c> for <c>CIV</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The country is not supported.</exception>
    public static string GetSenderAddress(string iso3CountryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iso3CountryCode);
        return SenderAddresses.TryGetValue(iso3CountryCode, out var address)
            ? address
            : throw new ArgumentException($"Country '{iso3CountryCode}' is not supported by the Orange SMS API.", nameof(iso3CountryCode));
    }
}

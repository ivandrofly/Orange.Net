namespace Orange.Net.Sms;

internal static class PhoneNumber
{
    private const int MaxSenderNameLength = 11;

    /// <summary>
    /// Normalises "+225 07 01 02 03", "002250701...", "225070102..." or "tel:+225..." to "tel:+225...".
    /// </summary>
    public static string ToTelUri(string number, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, paramName);

        var span = number.AsSpan().Trim();
        if (span.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            span = span[4..];

        Span<char> digits = stackalloc char[span.Length];
        var length = 0;
        foreach (var c in span)
        {
            if (char.IsAsciiDigit(c))
                digits[length++] = c;
            else if (c is not (' ' or '-' or '.' or '(' or ')' or '+'))
                throw new ArgumentException($"'{number}' is not a valid phone number.", paramName);
        }

        var result = digits[..length];
        if (result.StartsWith("00"))
            result = result[2..];

        if (result.Length is < 4 or > 15)
            throw new ArgumentException($"'{number}' is not a valid international phone number.", paramName);

        return string.Concat("tel:+", result);
    }

    public static void ValidateSenderName(string? senderName, string paramName)
    {
        if (senderName is null)
            return;

        if (senderName.Length is 0 or > MaxSenderNameLength
            || senderName.Trim().Length == 0
            || !senderName.All(c => char.IsAsciiLetterOrDigit(c) || c == ' '))
        {
            throw new ArgumentException(
                $"Sender name '{senderName}' is invalid: use 1-{MaxSenderNameLength} letters, digits or spaces.", paramName);
        }
    }
}

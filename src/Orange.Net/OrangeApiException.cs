using System.Net;

namespace Orange.Net;

/// <summary>
/// Thrown when an Orange API (or the OAuth token endpoint) returns a non-success status code.
/// </summary>
public sealed class OrangeApiException : Exception
{
    /// <summary>Creates a new <see cref="OrangeApiException"/>.</summary>
    public OrangeApiException(
        HttpStatusCode statusCode,
        string? errorCode,
        string? errorMessage,
        string? description,
        string? rawBody)
        : base(BuildMessage(statusCode, errorCode, errorMessage, description))
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Description = description;
        RawBody = rawBody;
    }

    /// <summary>HTTP status code of the failed response.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Orange error code: a gateway code such as <c>42</c> (expired credentials),
    /// an OAuth error such as <c>invalid_client</c>, or an SMS message ID such as <c>SVC0004</c>.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>Short error message returned by Orange.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Longer error description returned by Orange, when present.</summary>
    public string? Description { get; }

    /// <summary>The raw response body.</summary>
    public string? RawBody { get; }

    private static string BuildMessage(HttpStatusCode statusCode, string? code, string? message, string? description)
    {
        var text = $"Orange API returned {(int)statusCode} ({statusCode})";
        if (code is not null) text += $", code {code}";
        if (message is not null) text += $": {message}";
        if (description is not null && description != message) text += $" - {description}";
        return text;
    }
}

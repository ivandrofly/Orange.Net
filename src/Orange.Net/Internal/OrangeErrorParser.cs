using System.Text.Json;

namespace Orange.Net.Internal;

/// <summary>
/// Turns non-success responses into <see cref="OrangeApiException"/>. Understands the three error shapes Orange uses:
/// <list type="bullet">
///   <item>gateway: <c>{"code": 42, "message": "...", "description": "..."}</c></item>
///   <item>OAuth: <c>{"error": "invalid_client", "error_description": "..."}</c></item>
///   <item>OneAPI (SMS): <c>{"requestError": {"serviceException": {"messageId": "SVC0004", "text": "... %1", "variables": ["..."]}}}</c></item>
/// </list>
/// </summary>
internal static class OrangeErrorParser
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw CreateException(response, body);
    }

    internal static OrangeApiException CreateException(HttpResponseMessage response, string? body)
    {
        string? code = null, message = null, description = null;

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                    (code, message, description) = Parse(root);
            }
            catch (JsonException)
            {
                // Not JSON (e.g. an HTML error page from a proxy); keep the raw body only.
            }
        }

        message ??= response.ReasonPhrase;
        return new OrangeApiException(response.StatusCode, code, message, description, body);
    }

    private static (string? Code, string? Message, string? Description) Parse(JsonElement root)
    {
        if (root.TryGetProperty("requestError", out var requestError) && requestError.ValueKind == JsonValueKind.Object)
        {
            foreach (var kind in (ReadOnlySpan<string>)["serviceException", "policyException"])
            {
                if (requestError.TryGetProperty(kind, out var exception) && exception.ValueKind == JsonValueKind.Object)
                {
                    var text = GetString(exception, "text");
                    return (GetString(exception, "messageId"), ExpandVariables(text, exception), kind);
                }
            }
        }

        if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            return (error.GetString(), GetString(root, "error_description"), null);

        if (root.TryGetProperty("code", out _))
            return (GetString(root, "code"), GetString(root, "message"), GetString(root, "description"));

        return (null, GetString(root, "message"), GetString(root, "description"));
    }

    private static string? ExpandVariables(string? text, JsonElement exception)
    {
        if (text is null || !exception.TryGetProperty("variables", out var variables) || variables.ValueKind != JsonValueKind.Array)
            return text;

        var index = 1;
        foreach (var variable in variables.EnumerateArray())
            text = text.Replace($"%{index++}", variable.ToString());
        return text;
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                _ => value.GetRawText(),
            }
            : null;
}

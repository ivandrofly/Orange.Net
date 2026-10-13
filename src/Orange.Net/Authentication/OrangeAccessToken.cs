namespace Orange.Net.Authentication;

/// <summary>An OAuth 2.0 access token issued by Orange.</summary>
/// <param name="AccessToken">The bearer token value.</param>
/// <param name="TokenType">Token type, normally <c>Bearer</c>.</param>
/// <param name="Scope">Scopes granted to the application, when returned.</param>
/// <param name="ExpiresAt">When the token expires.</param>
public sealed record OrangeAccessToken(string AccessToken, string TokenType, string? Scope, DateTimeOffset ExpiresAt);

using System.Net;
using System.Text;

namespace Orange.Net.Tests;

/// <summary>A captured request, with its body read before the request is disposed.</summary>
public sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? Body, string? ContentType);

/// <summary>
/// Stand-in for the Orange gateway. Token requests get a fresh token ("token-1", "token-2", …);
/// API requests are answered by <see cref="ApiResponder"/>.
/// </summary>
public sealed class FakeOrangeHandler : HttpMessageHandler
{
    private int _tokenCount;

    public List<CapturedRequest> TokenRequests { get; } = [];

    public List<CapturedRequest> ApiRequests { get; } = [];

    public Func<CapturedRequest, HttpResponseMessage> ApiResponder { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

    public Func<int, HttpResponseMessage>? TokenResponder { get; set; }

    public TimeSpan TokenDelay { get; set; }

    public int TokenCount => Volatile.Read(ref _tokenCount);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var captured = new CapturedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.Authorization?.ToString(),
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
            request.Content?.Headers.ContentType?.MediaType);

        if (request.RequestUri!.AbsolutePath == "/oauth/v3/token")
        {
            lock (TokenRequests) TokenRequests.Add(captured);
            if (TokenDelay > TimeSpan.Zero)
                await Task.Delay(TokenDelay, cancellationToken);
            var n = Interlocked.Increment(ref _tokenCount);
            return TokenResponder?.Invoke(n)
                ?? Json(HttpStatusCode.OK, $$"""{"token_type":"Bearer","access_token":"token-{{n}}","expires_in":3600}""");
        }

        lock (ApiRequests) ApiRequests.Add(captured);
        return ApiResponder(captured);
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

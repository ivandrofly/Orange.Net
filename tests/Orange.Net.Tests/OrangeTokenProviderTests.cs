using System.Net;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using Orange.Net.Authentication;

namespace Orange.Net.Tests;

public class OrangeTokenProviderTests
{
    private readonly FakeOrangeHandler _handler = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private OrangeTokenProvider CreateProvider() => new(new HttpClient(_handler), TestOptions.Create(), _time);

    [Fact]
    public async Task Requests_token_with_basic_auth_and_client_credentials_grant()
    {
        var token = await CreateProvider().GetAccessTokenAsync();

        Assert.Equal("token-1", token);
        var request = Assert.Single(_handler.TokenRequests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.orange.test/oauth/v3/token", request.Uri.AbsoluteUri);
        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("my-client:my-secret"));
        Assert.Equal($"Basic {expected}", request.Authorization);
        Assert.Equal("grant_type=client_credentials", request.Body);
        Assert.Equal("application/x-www-form-urlencoded", request.ContentType);
    }

    [Fact]
    public async Task Reuses_token_until_refresh_skew_then_requests_a_new_one()
    {
        var provider = CreateProvider();

        Assert.Equal("token-1", await provider.GetAccessTokenAsync());
        _time.Advance(TimeSpan.FromSeconds(3600 - 61));
        Assert.Equal("token-1", await provider.GetAccessTokenAsync());
        _time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal("token-2", await provider.GetAccessTokenAsync());
        Assert.Equal(2, _handler.TokenCount);
    }

    [Fact]
    public async Task Accepts_expires_in_as_string()
    {
        _handler.TokenResponder = n => FakeOrangeHandler.Json(HttpStatusCode.OK,
            $$"""{"token_type":"Bearer","access_token":"token-{{n}}","expires_in":"120","scope":"ope:smsmessaging:v1:access"}""");
        var provider = CreateProvider();

        await provider.GetAccessTokenAsync();

        Assert.Equal(_time.GetUtcNow().AddSeconds(120), provider.CurrentToken!.ExpiresAt);
        Assert.Equal("ope:smsmessaging:v1:access", provider.CurrentToken.Scope);
    }

    [Fact]
    public async Task Concurrent_callers_share_a_single_token_request()
    {
        _handler.TokenDelay = TimeSpan.FromMilliseconds(100);
        var provider = CreateProvider();

        var tokens = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => provider.GetAccessTokenAsync().AsTask()));

        Assert.All(tokens, t => Assert.Equal("token-1", t));
        Assert.Equal(1, _handler.TokenCount);
    }

    [Fact]
    public async Task Invalidate_only_discards_the_matching_token()
    {
        var provider = CreateProvider();
        await provider.GetAccessTokenAsync();

        provider.Invalidate("some-older-token");
        Assert.Equal("token-1", await provider.GetAccessTokenAsync());

        provider.Invalidate("token-1");
        Assert.Equal("token-2", await provider.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Token_endpoint_error_throws_OrangeApiException()
    {
        _handler.TokenResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.Unauthorized,
            """{"error":"invalid_client","error_description":"The requested service needs credentials, but the ones provided were invalid."}""");

        var ex = await Assert.ThrowsAsync<OrangeApiException>(() => CreateProvider().GetAccessTokenAsync().AsTask());

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("invalid_client", ex.ErrorCode);
        Assert.StartsWith("The requested service needs credentials", ex.ErrorMessage);
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("id", " ")]
    public void Rejects_missing_credentials(string clientId, string clientSecret)
    {
        var options = TestOptions.Create();
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;

        Assert.Throws<InvalidOperationException>(() => new OrangeTokenProvider(new HttpClient(_handler), options));
    }
}

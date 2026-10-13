using System.Net;
using System.Text.Json;
using Orange.Net.Sms;

namespace Orange.Net.Tests;

public class OrangeSmsClientTests
{
    private const string ResourceId = "8c3a2b1e-4f5d-4e6a-9b7c-0d1e2f3a4b5c";

    private readonly FakeOrangeHandler _handler = new();
    private readonly OrangeSmsClient _client;

    public OrangeSmsClientTests()
    {
        _client = OrangeSmsClient.Create(TestOptions.Create(), _handler);
        _handler.ApiResponder = _ => Created();
    }

    private static HttpResponseMessage Created(string? body = null, bool location = true)
    {
        var response = FakeOrangeHandler.Json(HttpStatusCode.Created, body ?? "{}");
        if (location)
            response.Headers.Location = new Uri($"https://api.orange.test/smsmessaging/v1/outbound/tel:+2250000/requests/{ResourceId}");
        return response;
    }

    private static SmsMessage Message(string? senderName = null, bool onNet = false) => new()
    {
        To = "+225 07 01 02 03 04",
        Text = "Hello!",
        SenderAddress = OrangeSmsCountries.GetSenderAddress(OrangeSmsCountries.CoteDIvoire),
        SenderName = senderName,
        OnNet = onNet,
    };

    [Fact]
    public async Task SendAsync_posts_to_encoded_sender_url_with_bearer_token()
    {
        var result = await _client.SendAsync(Message());

        var request = Assert.Single(_handler.ApiRequests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.orange.test/smsmessaging/v1/outbound/tel%3A%2B2250000/requests", request.Uri.AbsoluteUri);
        Assert.Equal("Bearer token-1", request.Authorization);
        Assert.Equal("application/json", request.ContentType);
        Assert.Equal(ResourceId, result.ResourceId);
        Assert.Equal("tel:+2250701020304", result.Address);
        Assert.Equal("tel:+2250000", result.SenderAddress);
    }

    [Fact]
    public async Task SendAsync_body_matches_Orange_wire_format()
    {
        await _client.SendAsync(Message());

        using var json = JsonDocument.Parse(_handler.ApiRequests[0].Body!);
        var request = json.RootElement.GetProperty("outboundSMSMessageRequest");
        Assert.Equal("tel:+2250701020304", request.GetProperty("address").GetString());
        Assert.Equal("tel:+2250000", request.GetProperty("senderAddress").GetString());
        Assert.Equal("Hello!", request.GetProperty("outboundSMSTextMessage").GetProperty("message").GetString());
        Assert.False(request.TryGetProperty("senderName", out _));
    }

    [Fact]
    public async Task SendAsync_includes_sender_name_when_set()
    {
        var result = await _client.SendAsync(Message(senderName: "MonService"));

        using var json = JsonDocument.Parse(_handler.ApiRequests[0].Body!);
        Assert.Equal("MonService", json.RootElement.GetProperty("outboundSMSMessageRequest").GetProperty("senderName").GetString());
        Assert.Equal("MonService", result.SenderName);
    }

    [Fact]
    public async Task SendAsync_on_net_adds_offer_query_parameter()
    {
        await _client.SendAsync(Message(onNet: true));

        Assert.Equal("?resource_type_parameter_management=SMS_OCB2", _handler.ApiRequests[0].Uri.Query);
    }

    [Fact]
    public async Task SendAsync_falls_back_to_resourceURL_in_body()
    {
        _handler.ApiResponder = _ => Created(
            $$$"""{"outboundSMSMessageRequest":{"resourceURL":"https://api.orange.test/smsmessaging/v1/outbound/tel:+2250000/requests/{{{ResourceId}}}"}}""",
            location: false);

        var result = await _client.SendAsync(Message());

        Assert.Equal(ResourceId, result.ResourceId);
    }

    [Theory]
    [InlineData("TooLongName12")]
    [InlineData("Bad-Name!")]
    [InlineData("   ")]
    public async Task SendAsync_rejects_invalid_sender_name_without_calling_the_api(string senderName)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _client.SendAsync(Message(senderName)));

        Assert.Empty(_handler.ApiRequests);
        Assert.Equal(0, _handler.TokenCount);
    }

    [Fact]
    public async Task SendAsync_maps_OneAPI_service_exception()
    {
        _handler.ApiResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.BadRequest,
            """{"requestError":{"serviceException":{"messageId":"SVC0004","text":"No valid addresses provided in message part %1","variables":["address"]}}}""");

        var ex = await Assert.ThrowsAsync<OrangeApiException>(() => _client.SendAsync(Message()));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("SVC0004", ex.ErrorCode);
        Assert.Equal("No valid addresses provided in message part address", ex.ErrorMessage);
        Assert.Equal("serviceException", ex.Description);
    }

    [Fact]
    public async Task Gateway_error_is_mapped()
    {
        _handler.ApiResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.TooManyRequests,
            """{"code":53,"message":"Too Many Requests","description":"The application has made too many calls and has exceeded the rate limit for this service."}""");

        var ex = await Assert.ThrowsAsync<OrangeApiException>(() => _client.SendAsync(Message()));

        Assert.Equal("53", ex.ErrorCode);
        Assert.Equal("Too Many Requests", ex.ErrorMessage);
        Assert.StartsWith("The application has made too many calls", ex.Description);
    }

    [Fact]
    public async Task Expired_token_is_refreshed_and_request_retried_once()
    {
        _handler.ApiResponder = r => r.Authorization == "Bearer token-1"
            ? FakeOrangeHandler.Json(HttpStatusCode.Unauthorized, """{"code":42,"message":"Expired credentials"}""")
            : Created();

        var result = await _client.SendAsync(Message());

        Assert.Equal(ResourceId, result.ResourceId);
        Assert.Equal(2, _handler.TokenCount);
        Assert.Equal(["Bearer token-1", "Bearer token-2"], _handler.ApiRequests.Select(r => r.Authorization));
        Assert.Equal(_handler.ApiRequests[0].Body, _handler.ApiRequests[1].Body);
    }

    [Fact]
    public async Task Persistent_401_is_not_retried_forever()
    {
        _handler.ApiResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.Unauthorized,
            """{"code":42,"message":"Expired credentials","description":"The requested service needs credentials, and the ones provided were out-of-date."}""");

        var ex = await Assert.ThrowsAsync<OrangeApiException>(() => _client.SendAsync(Message()));

        Assert.Equal("42", ex.ErrorCode);
        Assert.Equal(2, _handler.ApiRequests.Count);
    }

    [Fact]
    public async Task GetContractsAsync_parses_contracts_and_filters_by_country()
    {
        _handler.ApiResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.OK, """
            [{
              "id": "c1", "type": "SELFSERVICE", "developerId": "dev", "applicationId": "app",
              "country": "CIV", "offerName": "SMS_OCB", "availableUnits": 120, "requestedUnits": 0,
              "status": "ACTIVE", "expirationDate": "2023-01-07T15:04:20.653Z",
              "creationDate": "2022-11-07T08:11:38.220Z", "lastUpdateDate": "2022-12-08T15:04:20.656Z"
            }]
            """);

        var contracts = await _client.GetContractsAsync("CIV");

        Assert.Equal("https://api.orange.test/sms/admin/v1/contracts?country=CIV", _handler.ApiRequests[0].Uri.AbsoluteUri);
        var contract = Assert.Single(contracts);
        Assert.Equal(120, contract.AvailableUnits);
        Assert.True(contract.IsActive);
        Assert.Equal(new DateTimeOffset(2023, 1, 7, 15, 4, 20, 653, TimeSpan.Zero), contract.ExpirationDate);
    }

    [Fact]
    public async Task GetStatisticsAsync_parses_nested_statistics()
    {
        _handler.ApiResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.OK, """
            {"partnerStatistics":{"developerId":"dev","statistics":[{"service":"SMS_OCB","serviceStatistics":[
              {"country":"CIV","countryStatistics":[{"appid":"app","usage":2,"nbEnforcements":1}]}]}]}}
            """);

        var stats = await _client.GetStatisticsAsync("CIV", "app");

        Assert.Equal("?country=CIV&appid=app", _handler.ApiRequests[0].Uri.Query);
        Assert.Equal("dev", stats.DeveloperId);
        var app = stats.Statistics.Single().Countries.Single().Applications.Single();
        Assert.Equal("app", app.ApplicationId);
        Assert.Equal(2, app.Usage);
        Assert.Equal(1, app.NbEnforcements);
    }

    [Fact]
    public async Task GetPurchaseOrdersAsync_parses_orders()
    {
        _handler.ApiResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.OK, """
            [{"id":"po1","country":"CIV","offerName":"SMS_OCB","bundleId":"b1","price":"5000","currency":"XOF",
              "purchaseDate":"2022-12-08T15:04:20.656Z","type":"BUNDLE_PURCHASE","oldAvailableUnits":0,"newAvailableUnits":100}]
            """);

        var orders = await _client.GetPurchaseOrdersAsync();

        Assert.Equal("https://api.orange.test/sms/admin/v1/purchaseorders", _handler.ApiRequests[0].Uri.AbsoluteUri);
        var order = Assert.Single(orders);
        Assert.Equal(5000m, order.Price);
        Assert.Equal(100, order.NewAvailableUnits);
    }
}

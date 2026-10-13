# Orange.Net

A .NET 10 client for the [Orange Developer](https://developer.orange.com) APIs.

It currently covers:

- **OAuth 2.0 (v3, client credentials):** tokens are cached and refreshed automatically, and a request that gets 401 (code 42, expired credentials) is retried once with a fresh token.
- **[SMS Middle East & Africa](https://developer.orange.com/apis/sms/getting-started):** send SMS, check contracts and balance, read usage statistics and purchase history, and parse delivery receipts.

The token and HTTP plumbing is shared, so you can add other Orange APIs (Orange Money, network APIs, …) as sibling clients. The library is trim- and AOT-compatible.

## Getting credentials

1. Create an app on https://developer.orange.com/myapps and subscribe it to **SMS Africa & Middle East**.
2. Copy its **Client ID** and **Client secret**.
3. Buy (or claim the free) SMS bundle for the country you want to send in.

## Usage with dependency injection

```csharp
builder.Services.AddOrangeSms(builder.Configuration.GetSection("Orange"));
// or: builder.Services.AddOrangeSms(o => { o.ClientId = "..."; o.ClientSecret = "..."; });
```

```json
// appsettings.json (keep the secret in user-secrets / a vault)
{ "Orange": { "ClientId": "…", "ClientSecret": "…" } }
```

```csharp
public sealed class Notifier(IOrangeSmsClient sms)
{
    public Task<SendSmsResult> SendAsync(string phone, string text) =>
        sms.SendAsync(new SmsMessage
        {
            To = phone,                                         // "+225…", "225…" or "tel:+225…"
            Text = text,
            SenderAddress = OrangeSmsCountries.GetSenderAddress(OrangeSmsCountries.CoteDIvoire), // "2250000"
            SenderName = "MyShop",                              // optional, must be whitelisted by Orange
        });
}
```

`AddOrangeSms` returns the `IHttpClientBuilder`, so you can chain resilience or rate limiting:

```csharp
builder.Services.AddOrangeSms(config.GetSection("Orange"))
    .AddStandardResilienceHandler();   // Microsoft.Extensions.Http.Resilience
```

## Usage without dependency injection

```csharp
var sms = OrangeSmsClient.Create(new OrangeClientOptions { ClientId = "…", ClientSecret = "…" });
// Create once and reuse: the instance owns its HttpClient and token cache.
```

## Account information

```csharp
var contracts = await sms.GetContractsAsync("CIV");      // remaining units, status, expiry
var stats     = await sms.GetStatisticsAsync("CIV");
var purchases = await sms.GetPurchaseOrdersAsync();
```

## Delivery receipts

Orange POSTs a receipt to your endpoint within 24 hours of each send. The endpoint must use HTTPS on port 443 with a CA-signed certificate. You register it through Orange's SMS API contact form, and Orange then gives you an IP address to allow. Your endpoint must answer `200 OK`.

```csharp
app.MapPost("/orange/sms-dr", async (HttpRequest request) =>
{
    var receipt = await DeliveryInfoNotification.ParseAsync(request.Body);
    // receipt.CallbackData == SendSmsResult.ResourceId
    // receipt.DeliveryInfo.Status: DeliveredToTerminal, DeliveryImpossible, …
    return Results.Ok();
});
```

## Errors

Every failed call throws `OrangeApiException`. It exposes `StatusCode`, `ErrorCode`, `ErrorMessage`, `Description` and `RawBody`. The client understands three error formats:

| Source | Example `ErrorCode` |
|---|---|
| API gateway | `42` (expired credentials), `50` (access denied), `53` (too many requests) |
| OAuth endpoint | `invalid_client` |
| SMS API (OneAPI) | `SVC0004`, `POL0001`; `ErrorMessage` has the `%1` placeholders filled in |

Invalid input (bad phone number, a sender name longer than 11 characters or with symbols) throws `ArgumentException` before any HTTP call.

## Limits to know

- Orange allows **5 SMS per second** per application. The client does not throttle, so chain a rate limiter if you send in bulk.
- Your contract must be `ACTIVE`, unexpired, and have units left.
- Supported countries are listed in `OrangeSmsCountries`: BWA, BFA, CMR, CIV, GIN, GNB, COD, JOR, LBR, MLI, MDG, SEN and TUN.

## Repository layout

```
src/Orange.Net/              the library (NuGet package)
tests/Orange.Net.Tests/      xUnit tests (no network, no credentials)
samples/Orange.Net.Sample/   console app: shows balance, optionally sends an SMS
```

```powershell
dotnet test
cd samples/Orange.Net.Sample
dotnet user-secrets set Orange:ClientId <id>
dotnet user-secrets set Orange:ClientSecret <secret>
dotnet run -- --country CIV                                  # balance only
dotnet run -- --country CIV --to +2250701020304 --text "Hi"  # sends one SMS (uses credit)
```

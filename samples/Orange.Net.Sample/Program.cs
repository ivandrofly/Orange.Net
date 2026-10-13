// Orange.Net sample: shows your SMS balance and, if --to is given, sends one SMS.
//
//   dotnet user-secrets set Orange:ClientId <id>         (or env var Orange__ClientId)
//   dotnet user-secrets set Orange:ClientSecret <secret>
//   dotnet run -- --country CIV
//   dotnet run -- --country CIV --to +2250701020304 --text "Hello from Orange.Net" [--sender-name MyShop]

using Microsoft.Extensions.Configuration;
using Orange.Net;
using Orange.Net.Sms;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var options = configuration.GetSection(OrangeClientOptions.SectionName).Get<OrangeClientOptions>() ?? new OrangeClientOptions();
if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
{
    Console.Error.WriteLine("Set Orange:ClientId and Orange:ClientSecret (user secrets or Orange__ClientId / Orange__ClientSecret env vars).");
    return 1;
}

var country = configuration["country"] ?? OrangeSmsCountries.CoteDIvoire;
var sms = OrangeSmsClient.Create(options);

try
{
    Console.WriteLine($"SMS contracts ({country}):");
    foreach (var contract in await sms.GetContractsAsync(country))
        Console.WriteLine($"  {contract.Country} {contract.OfferName}: {contract.AvailableUnits} units, {contract.Status}, expires {contract.ExpirationDate:yyyy-MM-dd}");

    if (configuration["to"] is { Length: > 0 } to)
    {
        var result = await sms.SendAsync(new SmsMessage
        {
            To = to,
            Text = configuration["text"] ?? "Hello from Orange.Net",
            SenderAddress = OrangeSmsCountries.GetSenderAddress(country),
            SenderName = configuration["sender-name"],
        });
        Console.WriteLine($"Sent to {result.Address}. Resource ID: {result.ResourceId}");
    }

    return 0;
}
catch (OrangeApiException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

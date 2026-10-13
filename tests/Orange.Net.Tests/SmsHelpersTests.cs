using System.Text.Json;
using Orange.Net.Sms;

namespace Orange.Net.Tests;

public class SmsHelpersTests
{
    [Theory]
    [InlineData("+2250701020304")]
    [InlineData("2250701020304")]
    [InlineData("002250701020304")]
    [InlineData("tel:+2250701020304")]
    [InlineData(" +225 07-01.02 (03) 04 ")]
    public void Phone_numbers_are_normalised_to_tel_uri(string input) =>
        Assert.Equal("tel:+2250701020304", PhoneNumber.ToTelUri(input, "to"));

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("+22")]
    [InlineData("+1234567890123456")]
    [InlineData("+225#0701")]
    public void Invalid_phone_numbers_are_rejected(string input) =>
        Assert.ThrowsAny<ArgumentException>(() => PhoneNumber.ToTelUri(input, "to"));

    [Theory]
    [InlineData(null)]
    [InlineData("MonService")]
    [InlineData("My Shop 123")]
    public void Valid_sender_names_are_accepted(string? name) => PhoneNumber.ValidateSenderName(name, "senderName");

    [Theory]
    [InlineData("civ", "2250000")]
    [InlineData("JOR", "9620000")]
    [InlineData("TUN", "2160000")]
    public void Sender_address_lookup(string country, string expected) =>
        Assert.Equal(expected, OrangeSmsCountries.GetSenderAddress(country));

    [Fact]
    public void Unsupported_country_is_rejected() =>
        Assert.Throws<ArgumentException>(() => OrangeSmsCountries.GetSenderAddress("FRA"));

    [Fact]
    public void All_countries_are_listed() => Assert.Equal(13, OrangeSmsCountries.All.Count);

    [Theory]
    [InlineData("DeliveredToNetwork", DeliveryStatus.DeliveredToNetwork)]
    [InlineData("DeliveryUncertain", DeliveryStatus.DeliveryUncertain)]
    [InlineData("DeliveryImpossible", DeliveryStatus.DeliveryImpossible)]
    [InlineData("MessageWaiting", DeliveryStatus.MessageWaiting)]
    [InlineData("DeliveredToTerminal", DeliveryStatus.DeliveredToTerminal)]
    [InlineData("SomethingNew", DeliveryStatus.Unknown)]
    [InlineData("42", DeliveryStatus.Unknown)]
    public void Delivery_receipts_are_parsed(string status, DeliveryStatus expected)
    {
        var json = $$"""
            {"deliveryInfoNotification":{"callbackData":"abc-123",
              "deliveryInfo":{"address":"tel:+2250701020304","deliveryStatus":"{{status}}"} } }
            """;

        var receipt = DeliveryInfoNotification.Parse(json);

        Assert.Equal("abc-123", receipt.CallbackData);
        Assert.Equal("tel:+2250701020304", receipt.DeliveryInfo!.Address);
        Assert.Equal(status, receipt.DeliveryInfo.DeliveryStatusRaw);
        Assert.Equal(expected, receipt.DeliveryInfo.Status);
    }

    [Fact]
    public async Task Delivery_receipt_can_be_parsed_from_a_stream()
    {
        using var stream = new MemoryStream("""{"deliveryInfoNotification":{"callbackData":"x","deliveryInfo":{"deliveryStatus":"MessageWaiting"}}}"""u8.ToArray());

        var receipt = await DeliveryInfoNotification.ParseAsync(stream);

        Assert.Equal(DeliveryStatus.MessageWaiting, receipt.DeliveryInfo!.Status);
    }

    [Fact]
    public void Delivery_receipt_without_wrapper_is_rejected() =>
        Assert.Throws<JsonException>(() => DeliveryInfoNotification.Parse("""{"callbackData":"x"}"""));

    [Theory]
    [InlineData("https://api.orange.com/smsmessaging/v1/outbound/tel:+2250000/requests/abc-123", "abc-123")]
    [InlineData("https://api.orange.com/smsmessaging/v1/outbound/tel%3A%2B2250000/requests/abc-123/", "abc-123")]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void Resource_id_is_extracted_from_url(string? url, string? expected) =>
        Assert.Equal(expected, OrangeSmsClient.ExtractResourceId(url));
}

namespace Orange.Net.Tests;

internal static class TestOptions
{
    public static OrangeClientOptions Create() => new()
    {
        ClientId = "my-client",
        ClientSecret = "my-secret",
        BaseAddress = new Uri("https://api.orange.test"),
    };
}

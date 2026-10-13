using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orange.Net.Authentication;
using Orange.Net.Sms;

namespace Orange.Net.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public async Task AddOrangeSms_wires_typed_client_with_authentication()
    {
        var handler = new FakeOrangeHandler
        {
            ApiResponder = _ => FakeOrangeHandler.Json(HttpStatusCode.OK, "[]"),
        };
        var services = new ServiceCollection();
        services.AddOrangeSms(o =>
            {
                o.ClientId = "id";
                o.ClientSecret = "secret";
                o.BaseAddress = new Uri("https://api.orange.test/");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddHttpClient(OrangeServiceCollectionExtensions.TokenHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IOrangeSmsClient>();
        await client.GetContractsAsync();
        await provider.GetRequiredService<IOrangeSmsClient>().GetContractsAsync();

        Assert.Equal(1, handler.TokenCount);
        Assert.All(handler.ApiRequests, r => Assert.Equal("Bearer token-1", r.Authorization));
        Assert.Same(provider.GetRequiredService<IOrangeTokenProvider>(), provider.GetRequiredService<IOrangeTokenProvider>());
    }

    [Fact]
    public void AddOrangeSms_binds_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Orange:ClientId"] = "from-config",
                ["Orange:ClientSecret"] = "secret",
                ["Orange:TokenRefreshSkew"] = "00:02:00",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOrangeSms(configuration.GetSection(OrangeClientOptions.SectionName));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OrangeClientOptions>>().Value;

        Assert.Equal("from-config", options.ClientId);
        Assert.Equal(TimeSpan.FromMinutes(2), options.TokenRefreshSkew);
        Assert.Equal("https://api.orange.com/", options.BaseAddress.AbsoluteUri);
    }

    [Fact]
    public void Missing_credentials_fail_options_validation()
    {
        var services = new ServiceCollection();
        services.AddOrangeSms(_ => { });
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOrangeSmsClient>());
        Assert.Contains("ClientId", ex.Message);
    }
}

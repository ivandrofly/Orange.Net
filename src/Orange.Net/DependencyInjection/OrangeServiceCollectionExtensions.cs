using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orange.Net;
using Orange.Net.Authentication;
using Orange.Net.Sms;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Orange API clients with <see cref="IServiceCollection"/>.</summary>
public static class OrangeServiceCollectionExtensions
{
    internal const string TokenHttpClientName = "Orange.Net.Token";

    /// <summary>
    /// Registers <see cref="IOrangeSmsClient"/> as a typed <see cref="HttpClient"/> with Orange OAuth handling.
    /// </summary>
    /// <returns>The client builder, so you can chain handlers such as <c>AddStandardResilienceHandler()</c>.</returns>
    public static IHttpClientBuilder AddOrangeSms(this IServiceCollection services, Action<OrangeClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOrangeCore().Configure(configure);
        return services.AddOrangeSmsClient();
    }

    /// <summary>
    /// Registers <see cref="IOrangeSmsClient"/>, binding <see cref="OrangeClientOptions"/> from a configuration section
    /// such as <c>configuration.GetSection("Orange")</c>.
    /// </summary>
    /// <returns>The client builder, so you can chain handlers such as <c>AddStandardResilienceHandler()</c>.</returns>
    public static IHttpClientBuilder AddOrangeSms(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOrangeCore().Bind(configuration);
        return services.AddOrangeSmsClient();
    }

    private static IHttpClientBuilder AddOrangeSmsClient(this IServiceCollection services) =>
        services
            .AddHttpClient<IOrangeSmsClient, OrangeSmsClient>((sp, client) =>
                client.BaseAddress = sp.GetRequiredService<IOptions<OrangeClientOptions>>().Value.NormalizedBaseAddress)
            .AddHttpMessageHandler<OrangeAuthenticationHandler>();

    // Shared by every Orange API client: options, one token cache per application, and the auth handler.
    private static OptionsBuilder<OrangeClientOptions> AddOrangeCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient(TokenHttpClientName);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<OrangeClientOptions>, OrangeClientOptionsValidator>());
        services.TryAddSingleton<IOrangeTokenProvider>(sp => new OrangeTokenProvider(
            () => sp.GetRequiredService<IHttpClientFactory>().CreateClient(TokenHttpClientName),
            sp.GetRequiredService<IOptions<OrangeClientOptions>>().Value,
            sp.GetService<TimeProvider>()));
        services.TryAddTransient<OrangeAuthenticationHandler>();

        return services.AddOptions<OrangeClientOptions>();
    }

    private sealed class OrangeClientOptionsValidator : IValidateOptions<OrangeClientOptions>
    {
        public ValidateOptionsResult Validate(string? name, OrangeClientOptions options)
        {
            try
            {
                options.Validate();
                return ValidateOptionsResult.Success;
            }
            catch (InvalidOperationException ex)
            {
                return ValidateOptionsResult.Fail(ex.Message);
            }
        }
    }
}

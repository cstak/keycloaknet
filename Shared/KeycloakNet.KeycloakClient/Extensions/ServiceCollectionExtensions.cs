using KeycloakNet.KeycloakClient.Abstractions;
using KeycloakNet.KeycloakClient.Options;
using KeycloakNet.KeycloakClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeycloakNet.KeycloakClient.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKeycloakClient(
        this IServiceCollection services,
        Action<KeycloakClientOptions> configureOptions)
    {
        services.AddOptions<KeycloakClientOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RegisterServices(services);
        return services;
    }

    public static IServiceCollection AddKeycloakClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = KeycloakClientOptions.SectionName)
    {
        services.AddOptions<KeycloakClientOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RegisterServices(services);
        return services;
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient<IKeycloakTokenService, KeycloakTokenService>();
        services.AddHttpClient<IKeycloakClient, KeycloakClient>();
    }
}


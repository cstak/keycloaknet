using FluentValidation;
using KeycloakNet.WebBff.Requests;
using KeycloakNet.WebBff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace KeycloakNet.WebBff.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Configures forwarded headers for running behind a reverse proxy/gateway.
    /// </summary>
    public static IServiceCollection AddForwardedHeadersConfiguration(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    /// <summary>
    /// Configures Keycloak Authentication (Cookie + OpenID Connect Authorization Code Flow + PKCE),
    /// Authorization, and HttpContextAccessor.
    /// </summary>
    public static IServiceCollection AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakConfig = configuration.GetSection("Authentication:Keycloak");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = "__KeycloakNet_BFF";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.HttpOnly = true;
            // options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        })
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Authority = keycloakConfig["Authority"];
            options.ClientId = keycloakConfig["ClientId"];
            options.ClientSecret = keycloakConfig["ClientSecret"];
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.ResponseMode = OpenIdConnectResponseMode.Query;
            options.RequireHttpsMetadata = keycloakConfig.GetValue<bool>("RequireHttpsMetadata");

            options.SaveTokens = true; // Stores access and refresh tokens inside the encrypted cookie
            options.GetClaimsFromUserInfoEndpoint = true;
            options.MapInboundClaims = false;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");

            options.CallbackPath = "/bff/signin-oidc";
            options.SignedOutCallbackPath = "/bff/signout-callback-oidc";

            options.Events = new OpenIdConnectEvents
            {
                OnPushAuthorization = context =>
                {
                    // Workaround for Keycloak Issue #49255: Keycloak fails to decode prompt=create from PAR request_uri,
                    // resulting in a fallback to login-actions/authenticate.
                    // Skip PAR only for registration so that prompt=create is sent via front-channel query string.
                    if (context.Properties.Items.ContainsKey("kc_action") || context.Properties.Items.ContainsKey("prompt"))
                    {
                        context.SkipPush();
                    }

                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProvider = context =>
                {
                    if (context.Properties.Items.TryGetValue("kc_action", out var kcAction) && !string.IsNullOrEmpty(kcAction))
                    {
                        context.ProtocolMessage.SetParameter("kc_action", kcAction);
                    }

                    if (context.Properties.Items.TryGetValue("prompt", out var prompt) && !string.IsNullOrEmpty(prompt))
                    {
                        context.ProtocolMessage.Prompt = prompt;
                    }

                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProviderForSignOut = async context =>
                {
                    if (string.IsNullOrEmpty(context.ProtocolMessage.IdTokenHint))
                    {
                        var idToken = await context.HttpContext.GetTokenAsync("id_token");
                        if (!string.IsNullOrEmpty(idToken))
                        {
                            context.ProtocolMessage.IdTokenHint = idToken;
                        }
                    }
                }
            };
        });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators from assembly.
    /// </summary>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateArticleRequestValidator>();

        return services;
    }

    /// <summary>
    /// Configures typed HTTP clients for downstream services with token relay handler.
    /// </summary>
    public static IServiceCollection AddCoreServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        var coreServiceUrl = configuration["Services:CoreService"]
         ?? throw new InvalidOperationException("Required configuration 'Services:CoreService' is missing.");

        services.AddHttpClient<ITokenRefreshService, TokenRefreshService>();
        services.AddTransient<TokenRelayHandler>();

        services.AddHttpClient<ICoreServiceClient, CoreServiceClient>(client =>
        {
            client.BaseAddress = new Uri(coreServiceUrl);
        })
        .AddHttpMessageHandler<TokenRelayHandler>();

        return services;
    }
}


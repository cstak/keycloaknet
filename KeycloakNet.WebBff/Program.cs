using FluentValidation;
using KeycloakNet.WebBff.Endpoints;
using KeycloakNet.WebBff.Requests;
using KeycloakNet.WebBff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Acknowledge forwarded host and proto headers when running behind Gateway
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var keycloakConfig = builder.Configuration.GetSection("Authentication:Keycloak");

// 1. Authentication: Cookie + OpenIdConnect (Keycloak Authorization Code Flow + PKCE)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = "__KeycloakNet_BFF";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
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

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Register FluentValidation Validators
builder.Services.AddValidatorsFromAssemblyContaining<CreateArticleRequestValidator>();

// 2. Downstream Core Service Typed Client with Aspire Service Discovery & Token Relay Handler
var coreServiceUrl = builder.Configuration["Services:CoreService"] ?? "http://core-service";
builder.Services.AddHttpClient<ITokenRefreshService, TokenRefreshService>();
builder.Services.AddTransient<TokenRelayHandler>();

builder.Services.AddHttpClient<ICoreServiceClient, CoreServiceClient>(client =>
{
    client.BaseAddress = new Uri(coreServiceUrl);
})
.AddHttpMessageHandler<TokenRelayHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();

// 3. BFF Modular Endpoints
app.MapAuthEndpoints();
app.MapTestEndpoints();

app.MapDefaultEndpoints();

app.Run();

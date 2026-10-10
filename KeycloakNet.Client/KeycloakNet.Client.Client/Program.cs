using KeycloakNet.Client.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var gatewayUrl = builder.Configuration["Gateway:BaseUrl"]
    ?? throw new InvalidOperationException("Required configuration 'Gateway:BaseUrl' is missing.");


builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(gatewayUrl.TrimEnd('/') + "/");
});

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, HostAuthenticationStateProvider>();

await builder.Build().RunAsync();

var builder = WebApplication.CreateBuilder(args);

// Enable credentialed CORS for Blazor Client
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]
    ?? throw new InvalidOperationException("Required configuration 'Cors:AllowedOrigin' is missing.");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors();

app.MapReverseProxy();

app.Run();

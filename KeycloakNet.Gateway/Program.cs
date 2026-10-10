var builder = WebApplication.CreateBuilder(args);

// Enable credentialed CORS for Blazor Client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5300")
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

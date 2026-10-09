using KeycloakNet.KeycloakClient.Extensions;
using KeycloakNet.Service.Authentication;
using KeycloakNet.Service.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Keycloak Authentication & Authorization
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Programmatic Keycloak Admin Client (Headless Registration / User Management)
builder.Services.AddKeycloakClient(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

// Modular Endpoints
app.MapTestEndpoints();
app.MapAdminEndpoints();

app.Run();

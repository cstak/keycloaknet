using KeycloakNet.WebBff.Endpoints;
using KeycloakNet.WebBff.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Configure services & dependencies
builder.Services.AddForwardedHeadersConfiguration();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddValidationServices();
builder.Services.AddCoreServiceClient(builder.Configuration);

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

// BFF Modular Endpoints
app.MapAuthEndpoints();
app.MapTestEndpoints();

app.MapDefaultEndpoints();

app.Run();

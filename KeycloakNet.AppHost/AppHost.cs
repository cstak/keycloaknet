using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
var config = builder.Configuration;

string GetRequired(string key) =>
    config[key] ?? throw new InvalidOperationException($"Required configuration '{key}' is missing.");

int GetRequiredInt(string key) =>
    config.GetValue<int?>(key) ?? throw new InvalidOperationException($"Required configuration '{key}' is missing.");

// Database and Keycloak Configuration
var dbName = GetRequired("Database:Name");
var dbPasswordVal = GetRequired("Database:Password");

var kcPort = GetRequiredInt("Keycloak:Port");
var kcImage = GetRequired("Keycloak:Image");
var kcTag = GetRequired("Keycloak:Tag");
var kcAdmin = GetRequired("Keycloak:AdminUsername");
var kcAdminPass = GetRequired("Keycloak:AdminPassword");
var realmExportRelPath = GetRequired("Keycloak:RealmExportPath");
var realmExportPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, realmExportRelPath));

// Centralized Keycloak Settings (Common, Bff, Service)
var realm = GetRequired("Keycloak:Realm");

// BFF Settings
var bffClientId = GetRequired("Keycloak:Bff:ClientId");
var bffClientSecret = GetRequired("Keycloak:Bff:ClientSecret");

// Service Settings
var audience = GetRequired("Keycloak:Service:Audience");
var adminClientId = GetRequired("Keycloak:Service:AdminClientId");
var adminClientSecret = GetRequired("Keycloak:Service:AdminClientSecret");

// External Service Port Configuration
var gatewayPort = GetRequiredInt("Endpoints:GatewayPort");
var clientPort = GetRequiredInt("Endpoints:ClientPort");

// PostgreSQL Container
var dbPassword = builder.AddParameter("db-password", dbPasswordVal);
var postgres = builder.AddPostgres("postgres", password: dbPassword)
                      .WithDataVolume("keycloak-pgdata")
                      .AddDatabase("keycloak-db", dbName);

var dbUrl = $"jdbc:postgresql://postgres:5432/{dbName}";

// Keycloak Container (connected to PostgreSQL with automated realm import)
var keycloak = builder.AddContainer("keycloak", kcImage, kcTag)
                      .WithHttpEndpoint(port: kcPort, targetPort: kcPort, name: "http")
                      .WithReference(postgres)
                      .WithEnvironment("KC_DB", "postgres")
                      .WithEnvironment("KC_DB_URL", dbUrl)
                      .WithEnvironment("KC_DB_USERNAME", "postgres")
                      .WithEnvironment("KC_DB_PASSWORD", dbPasswordVal)
                      .WithEnvironment("KEYCLOAK_ADMIN", kcAdmin)
                      .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", kcAdminPass)
                      .WithEnvironment("KC_HTTP_PORT", kcPort.ToString())
                      .WithBindMount(realmExportPath, "/opt/keycloak/data/import/realm-export.json", isReadOnly: true)
                      .WithArgs("start-dev", "--import-realm")
                      .WaitFor(postgres);

var keycloakEndpoint = keycloak.GetEndpoint("http");
var keycloakAuthority = ReferenceExpression.Create($"{keycloakEndpoint}/realms/{realm}");

// Resource Server (Core Service) - Internal (Dynamic Port)
var coreService = builder.AddProject<Projects.KeycloakNet_Service>("core-service")
                         .WithEnvironment("Authentication__Keycloak__Authority", keycloakAuthority)
                         .WithEnvironment("Authentication__Keycloak__Audience", audience)
                         .WithEnvironment("Authentication__Keycloak__RequireHttpsMetadata", "false")
                         .WithEnvironment("KeycloakClient__AuthServerUrl", keycloakEndpoint)
                         .WithEnvironment("KeycloakClient__Realm", realm)
                         .WithEnvironment("KeycloakClient__AdminRealm", realm)
                         .WithEnvironment("KeycloakClient__ClientId", adminClientId)
                         .WithEnvironment("KeycloakClient__ClientSecret", adminClientSecret)
                         .WaitFor(keycloak);

// WebBff (OIDC & Token Relay) - Internal (Dynamic Port)
var webBff = builder.AddProject<Projects.KeycloakNet_WebBff>("web-bff")
                    .WithEnvironment("Authentication__Keycloak__Authority", keycloakAuthority)
                    .WithEnvironment("Authentication__Keycloak__ClientId", bffClientId)
                    .WithEnvironment("Authentication__Keycloak__ClientSecret", bffClientSecret)
                    .WithEnvironment("Authentication__Keycloak__RequireHttpsMetadata", "false")
                    .WithEnvironment("Services__CoreService", coreService.GetEndpoint("http"))
                    .WithReference(coreService)
                    .WaitFor(keycloak)
                    .WaitFor(coreService);

// Blazor Hosted WASM (Host Server) - External
var client = builder.AddProject<Projects.KeycloakNet_Client>("client")
                    .WithHttpEndpoint(port: clientPort, name: "http");

// API Gateway (YARP) - External
var gateway = builder.AddProject<Projects.KeycloakNet_Gateway>("gateway")
                     .WithHttpEndpoint(port: gatewayPort, name: "http")
                     .WithEnvironment("Cors__AllowedOrigin", client.GetEndpoint("http"))
                     .WithReference(webBff)
                     .WaitFor(webBff);

client.WaitFor(gateway);

builder.Build().Run();

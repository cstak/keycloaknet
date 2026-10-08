var builder = DistributedApplication.CreateBuilder(args);

var service = builder.AddProject<Projects.KeycloakNet_Service>("service")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.KeycloakNet_WebBff>("web-bff")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(service)
    .WaitFor(service);

builder.Build().Run();

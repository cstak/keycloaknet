using KeycloakNet.KeycloakClient.Abstractions;
using KeycloakNet.KeycloakClient.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KeycloakNet.Service.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/admin").RequireAuthorization("admin");
        var usersGroup = adminGroup.MapGroup("/users");

        // List users from Keycloak realm via Admin REST API
        usersGroup.MapGet("/", async (IKeycloakClient kcClient, CancellationToken ct) =>
        {
            var users = await kcClient.GetUsersAsync(max: 100, first: 0, ct);
            var response = users.Select(u => new AdminUserResponse(
                Id: u.Id,
                Username: u.Username,
                Email: u.Email,
                FirstName: u.FirstName,
                LastName: u.LastName,
                Enabled: u.Enabled,
                CreatedTimestamp: u.CreatedTimestamp.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(u.CreatedTimestamp.Value)
                    : null
            )).ToList();

            return Results.Ok(response);
        });

        // Programmatic User Creation with Credentials and Role Assignment
        usersGroup.MapPost("/", async (CreateAdminUserRequest request, IKeycloakClient kcClient, CancellationToken ct) =>
        {
            var createKeycloakReq = new CreateKeycloakUserRequest
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Enabled = true,
                EmailVerified = true,
                Credentials =
                [
                    new KeycloakCredentialDto
                    {
                        Type = "password",
                        Value = request.Password,
                        Temporary = false
                    }
                ]
            };

            var userId = await kcClient.CreateUserAsync(createKeycloakReq, ct);

            if (request.Roles is { Count: > 0 })
            {
                foreach (var role in request.Roles.Where(r => !string.IsNullOrWhiteSpace(r)))
                {
                    await kcClient.AssignRealmRoleAsync(userId, role, ct);
                }
            }

            var createdUser = await kcClient.GetUserByIdAsync(userId, ct);

            var response = new AdminUserResponse(
                Id: createdUser?.Id ?? userId,
                Username: createdUser?.Username ?? request.Username,
                Email: createdUser?.Email ?? request.Email,
                FirstName: createdUser?.FirstName ?? request.FirstName,
                LastName: createdUser?.LastName ?? request.LastName,
                Enabled: createdUser?.Enabled ?? true,
                CreatedTimestamp: createdUser?.CreatedTimestamp.HasValue == true
                    ? DateTimeOffset.FromUnixTimeMilliseconds(createdUser.CreatedTimestamp.Value)
                    : DateTimeOffset.UtcNow
            );

            return Results.Created($"/api/admin/users/{userId}", response);
        });

        // Programmatic User Deletion
        usersGroup.MapDelete("/{id}", async (string id, IKeycloakClient kcClient, CancellationToken ct) =>
        {
            var success = await kcClient.DeleteUserAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });

        // List Realm Roles dynamically from Keycloak
        adminGroup.MapGet("/roles", async (IKeycloakClient kcClient, CancellationToken ct) =>
        {
            var roles = await kcClient.GetRealmRolesAsync(ct);
            var response = roles.Select(r => new AdminRoleResponse(r.Id, r.Name, r.Description)).ToList();
            return Results.Ok(response);
        });

        return app;
    }
}

public record CreateAdminUserRequest(
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string Password,
    List<string>? Roles = null
);

public record AdminUserResponse(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled,
    DateTimeOffset? CreatedTimestamp
);

public record AdminRoleResponse(string Id, string Name, string? Description);



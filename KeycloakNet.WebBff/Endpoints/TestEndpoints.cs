using FluentValidation;
using KeycloakNet.WebBff.Requests;
using KeycloakNet.WebBff.Responses;
using KeycloakNet.WebBff.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KeycloakNet.WebBff.Endpoints;

public static class TestEndpoints
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bff/api");

        // 1. Public Endpoint - Anonymous Access
        group.MapGet("/public", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.GetPublicAsync(ct);
            return Results.Ok(result);
        });

        // 2. Protected Status Endpoint - Authenticated Access
        group.MapGet("/protected", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.GetProtectedAsync(ct);
            return Results.Ok(result);
        })
        .RequireAuthorization();

        // 3. User About Endpoint - Authenticated User Profile & Roles (Replaces obsolete /me)
        group.MapGet("/user/about", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.GetUserAboutAsync(ct);
            return result is not null ? Results.Ok(result) : Results.Unauthorized();
        })
        .RequireAuthorization();

        // 4. Create Article Endpoint - Standard FluentValidation (IValidator<T>) + Token Relay
        group.MapPost("/articles", async (
            CreateArticleRequest request,
            IValidator<CreateArticleRequest> validator,
            ICoreServiceClient coreServiceClient,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await coreServiceClient.CreateArticleAsync(request, ct);
            return result is not null 
                ? Results.Created($"/bff/api/articles/{result.Id}", result) 
                : Results.BadRequest(new { error = "Failed to create article." });
        })
        .RequireAuthorization();

        // 5. Delete Article Endpoint
        group.MapDelete("/articles/{id:guid}", async (Guid id, ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var success = await coreServiceClient.DeleteArticleAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization();

        // 6. Admin User Management - List Users via Keycloak REST API
        group.MapGet("/admin/users", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var users = await coreServiceClient.GetAdminUsersAsync(ct);
            return Results.Ok(users ?? []);
        })
        .RequireAuthorization();

        // 7. Admin User Management - Programmatic User Creation with FluentValidation
        group.MapPost("/admin/users", async (
            CreateAdminUserRequest request,
            IValidator<CreateAdminUserRequest> validator,
            ICoreServiceClient coreServiceClient,
            CancellationToken ct) =>
        {
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await coreServiceClient.CreateAdminUserAsync(request, ct);
            return result is not null
                ? Results.Created($"/bff/api/admin/users/{result.Id}", result)
                : Results.BadRequest(new { error = "Failed to create user in Keycloak." });
        })
        .RequireAuthorization();

        // 8. Admin User Management - Delete User
        group.MapDelete("/admin/users/{id}", async (string id, ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var success = await coreServiceClient.DeleteAdminUserAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization();

        // 9. Admin Role Management - List Realm Roles via Keycloak REST API
        group.MapGet("/admin/roles", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var roles = await coreServiceClient.GetAdminRolesAsync(ct);
            return Results.Ok(roles ?? []);
        })
        .RequireAuthorization();

        return app;
    }
}

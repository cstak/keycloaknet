using System.Net;
using FluentValidation;
using KeycloakNet.WebBff.Requests;
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
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : ToErrorResult(result.StatusCode, result.Error);
        });

        // 2. Protected Status Endpoint - Authenticated Access
        group.MapGet("/protected", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.GetProtectedAsync(ct);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : ToErrorResult(result.StatusCode, result.Error);
        })
        .RequireAuthorization();

        // 3. User About Endpoint - Authenticated User Profile & Roles
        group.MapGet("/user/about", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.GetUserAboutAsync(ct);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : ToErrorResult(result.StatusCode, result.Error);
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
            return result.IsSuccess && result.Data is not null
                ? Results.Created($"/bff/api/articles/{result.Data.Id}", result.Data)
                : ToErrorResult(result.StatusCode, result.Error);
        })
        .RequireAuthorization();

        // 5. Delete Article Endpoint
        group.MapDelete("/articles/{id:guid}", async (Guid id, ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.DeleteArticleAsync(id, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : ToErrorResult(result.StatusCode, result.Error);
        })
        .RequireAuthorization();

        // 6. Admin User Management - List Users via Keycloak REST API
        group.MapGet("/admin/users", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.GetAdminUsersAsync(ct);
            return result.IsSuccess
                ? Results.Ok(result.Data ?? [])
                : ToErrorResult(result.StatusCode, result.Error);
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
            return result.IsSuccess && result.Data is not null
                ? Results.Created($"/bff/api/admin/users/{result.Data.Id}", result.Data)
                : ToErrorResult(result.StatusCode, result.Error);
        })
        .RequireAuthorization();

        // 8. Admin User Management - Delete User
        group.MapDelete("/admin/users/{id}", async (string id, ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.DeleteAdminUserAsync(id, ct);
            return result.IsSuccess
                ? Results.NoContent()
                : ToErrorResult(result.StatusCode, result.Error);
        })
        .RequireAuthorization();

        // 9. Admin Role Management - List Realm Roles via Keycloak REST API
        group.MapGet("/admin/roles", async (ICoreServiceClient coreServiceClient, CancellationToken ct) =>
        {
            var result = await coreServiceClient.GetAdminRolesAsync(ct);
            return result.IsSuccess
                ? Results.Ok(result.Data ?? [])
                : ToErrorResult(result.StatusCode, result.Error);
        })
        .RequireAuthorization();

        return app;
    }

    private static IResult ToErrorResult(HttpStatusCode statusCode, string? error = null)
    {
        var message = !string.IsNullOrWhiteSpace(error)
            ? error
            : statusCode switch
            {
                HttpStatusCode.Unauthorized => "Unauthorized: Authentication is required.",
                HttpStatusCode.Forbidden => "Forbidden: You do not have permission to perform this action.",
                HttpStatusCode.NotFound => "Resource not found.",
                _ => $"Request failed with status {(int)statusCode} ({statusCode})."
            };

        return Results.Json(new { error = message }, statusCode: (int)statusCode);
    }
}

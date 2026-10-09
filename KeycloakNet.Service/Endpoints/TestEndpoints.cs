using System.Security.Claims;
using System.Text.Json;
using KeycloakNet.Service.Common.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KeycloakNet.Service.Endpoints;

public static class TestEndpoints
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        // Public Endpoint - Anonymous Access
        app.MapGet("/api/public", () => Results.Ok(new MessageResponse("This endpoint is public. No token required.")));

        // Protected Endpoint - Authenticated Access
        app.MapGet("/api/protected", () => Results.Ok(new MessageResponse("Authentication successful! You are in the secure area.")))
           .RequireAuthorization();

        // User About Endpoint - Authenticated User Profile & Roles (Replaces obsolete /api/me)
        app.MapGet("/api/user/about", (IUserContext userContext, HttpContext httpContext) =>
        {
            if (!userContext.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var principal = httpContext.User;
            var username = principal.FindFirst("preferred_username")?.Value
                        ?? principal.FindFirst("name")?.Value
                        ?? principal.Identity?.Name;

            var roles = new List<string>();
            var realmAccessClaim = principal.FindFirst("realm_access")?.Value;
            if (!string.IsNullOrWhiteSpace(realmAccessClaim))
            {
                try
                {
                    using var document = JsonDocument.Parse(realmAccessClaim);
                    if (document.RootElement.TryGetProperty("roles", out var rolesElement) &&
                        rolesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in rolesElement.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            if (!string.IsNullOrWhiteSpace(roleName))
                            {
                                roles.Add(roleName);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignore malformed realm_access payload
                }
            }

            return Results.Ok(new UserAboutResponse(
                UserId: userContext.UserId,
                Username: username,
                IsAuthenticated: userContext.IsAuthenticated,
                Roles: roles
            ));
        })
        .RequireAuthorization();

        // Articles List (Sample In-Memory or Demo)
        app.MapGet("/api/articles", () => Results.Ok(new List<ArticleResponse>
        {
            new(
                Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title: ".NET 10 & Keycloak Architecture",
                Content: "Token security and identity management with the BFF pattern.",
                AuthorId: Guid.Empty,
                CreatedAtUtc: DateTime.UtcNow.AddHours(-1)
            )
        }))
        .RequireAuthorization();

        // Authorized Article Creation (stamps AuthorId from Keycloak token)
        app.MapPost("/api/articles", (CreateArticleRequest request, IUserContext userContext) =>
        {
            var article = new ArticleResponse(
                Id: Guid.NewGuid(),
                Title: request.Title,
                Content: request.Content,
                AuthorId: userContext.UserId,
                CreatedAtUtc: DateTime.UtcNow
            );
            return Results.Created($"/api/articles/{article.Id}", article);
        })
        .RequireAuthorization("articles:create");

        // Authorized Article Deletion
        app.MapDelete("/api/articles/{id:guid}", (Guid id) => Results.NoContent())
           .RequireAuthorization("articles:delete");

        return app;
    }
}

public record MessageResponse(string Message);
public record UserAboutResponse(Guid UserId, string? Username, bool IsAuthenticated, List<string> Roles);
public record CreateArticleRequest(string Title, string Content);
public record ArticleResponse(Guid Id, string Title, string Content, Guid AuthorId, DateTime CreatedAtUtc);

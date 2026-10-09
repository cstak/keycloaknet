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
        app.MapGet("/api/user/about", (IUserContext userContext) =>
        {
            if (!userContext.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new UserAboutResponse(
                UserId: userContext.UserId,
                Username: userContext.Username,
                IsAuthenticated: userContext.IsAuthenticated,
                Roles: userContext.Roles.ToList()
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

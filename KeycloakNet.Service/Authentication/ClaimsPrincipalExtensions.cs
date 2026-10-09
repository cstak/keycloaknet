using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KeycloakNet.Service.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        string? userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal?.FindFirstValue("sub");

        return Guid.TryParse(userId, out Guid parsedUserId) ?
            parsedUserId :
            throw new ApplicationException("User id is unavailable");
    }

    public static string? GetUsername(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirst("preferred_username")?.Value
            ?? principal?.FindFirst("name")?.Value
            ?? principal?.Identity?.Name;
    }

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal? principal, ILogger? logger = null)
    {
        if (principal is null)
        {
            return [];
        }

        var roles = new List<string>();

        // 1. Standard role claims
        roles.AddRange(principal.FindAll(ClaimTypes.Role).Select(c => c.Value));
        roles.AddRange(principal.FindAll("role").Select(c => c.Value));
        roles.AddRange(principal.FindAll("roles").Select(c => c.Value));

        // 2. Keycloak realm_access nested JSON claim
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
            catch (JsonException ex)
            {
                logger?.LogError(ex, "Failed to parse Keycloak 'realm_access' claim JSON. Roles could not be loaded for user {Subject}.",
                    principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown");
            }
        }

        return roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
    }
}


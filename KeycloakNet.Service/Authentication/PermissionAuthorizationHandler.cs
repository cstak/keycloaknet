using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace KeycloakNet.Service.Authentication;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // Keycloak embeds realm roles inside the nested "realm_access" claim JSON
        var realmAccessClaim = context.User.FindFirst("realm_access")?.Value;

        if (string.IsNullOrWhiteSpace(realmAccessClaim))
        {
            return Task.CompletedTask;
        }

        try
        {
            using var document = JsonDocument.Parse(realmAccessClaim);

            if (document.RootElement.TryGetProperty("roles", out var rolesElement) &&
                rolesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in rolesElement.EnumerateArray())
                {
                    if (string.Equals(role.GetString(), requirement.Permission, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Gracefully ignore malformed JSON payloads
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}


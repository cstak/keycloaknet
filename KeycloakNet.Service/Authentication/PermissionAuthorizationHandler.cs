using KeycloakNet.Service.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace KeycloakNet.Service.Authentication;

public sealed class PermissionAuthorizationHandler(IUserContext userContext) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (userContext.IsAuthenticated && userContext.IsInRole(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}


using KeycloakNet.Service.Authentication;
using KeycloakNet.Service.Common.Exceptions;
using KeycloakNet.Service.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace KeycloakNet.Service.Common;

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId =>
        httpContextAccessor
            .HttpContext?
            .User
            .GetUserId() ??
        throw new UserContextUnavailableException();

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}


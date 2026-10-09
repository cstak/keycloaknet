using System.Security.Claims;
using KeycloakNet.Service.Authentication;
using KeycloakNet.Service.Common.Exceptions;
using KeycloakNet.Service.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KeycloakNet.Service.Common;

public sealed class UserContext(
    IHttpContextAccessor httpContextAccessor,
    ILogger<UserContext> logger) : IUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        User.GetUserId();

    public string? Username =>
        User.GetUsername();

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        User.GetRoles(logger);

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}


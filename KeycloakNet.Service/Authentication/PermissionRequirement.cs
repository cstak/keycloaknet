using Microsoft.AspNetCore.Authorization;

namespace KeycloakNet.Service.Authentication;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}


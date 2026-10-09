using Microsoft.AspNetCore.Authorization;

namespace KeycloakNet.Service.Authentication;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(policy: permission)
{
    public string Permission { get; } = permission;
}


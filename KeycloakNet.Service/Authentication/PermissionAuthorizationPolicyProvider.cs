using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace KeycloakNet.Service.Authentication;

public class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is null)
        {
            var permissionRequirement = new PermissionRequirement(policyName);
            policy = new AuthorizationPolicyBuilder()
                .AddRequirements(permissionRequirement)
                .Build();
        }
        return policy;
    }
}


using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace KeycloakNet.Client.Client.Services;

public class HostAuthenticationStateProvider(ApiClient apiClient) : AuthenticationStateProvider
{
    private const string AuthenticationType = nameof(HostAuthenticationStateProvider);
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var userInfo = await apiClient.GetUserInfoAsync();
        if (userInfo == null || !userInfo.IsAuthenticated)
        {
            return Anonymous;
        }

        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(userInfo.Username))
        {
            claims.Add(new Claim(ClaimTypes.Name, userInfo.Username));
        }

        if (!string.IsNullOrEmpty(userInfo.UserId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userInfo.UserId));
            claims.Add(new Claim("sub", userInfo.UserId));
        }

        foreach (var c in userInfo.Claims)
        {
            claims.Add(new Claim(c.Type, c.Value));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationType);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthenticationChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}


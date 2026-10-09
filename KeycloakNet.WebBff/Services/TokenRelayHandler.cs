using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace KeycloakNet.WebBff.Services;

/// <summary>
/// Automatically extracts the Keycloak access token from the active HttpOnly encrypted cookie session
/// and injects it as an 'Authorization: Bearer <token>' header for downstream service calls (Zero-Trust Token Relay).
/// </summary>
public class TokenRelayHandler(
    IHttpContextAccessor httpContextAccessor,
    ITokenRefreshService tokenRefreshService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var accessToken = await tokenRefreshService.GetOrRefreshAccessTokenAsync(httpContext, cancellationToken);
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}


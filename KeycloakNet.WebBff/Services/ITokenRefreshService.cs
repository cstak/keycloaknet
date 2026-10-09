using Microsoft.AspNetCore.Http;

namespace KeycloakNet.WebBff.Services;

public interface ITokenRefreshService
{
    Task<string?> GetOrRefreshAccessTokenAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}


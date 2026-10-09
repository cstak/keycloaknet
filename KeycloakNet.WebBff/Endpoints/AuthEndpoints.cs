using System.Security.Claims;
using KeycloakNet.WebBff.DTOs;
using KeycloakNet.WebBff.Responses;
using KeycloakNet.WebBff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KeycloakNet.WebBff.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/login", (string? returnUrl, HttpContext httpContext) =>
        {
            var targetUrl = !string.IsNullOrWhiteSpace(returnUrl)
                ? returnUrl
                : httpContext.Request.Headers.Referer.ToString();

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                targetUrl = "/";
            }

            return Results.Challenge(new AuthenticationProperties { RedirectUri = targetUrl }, [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        app.MapGet("/bff/register", (string? returnUrl, HttpContext httpContext) =>
        {
            var targetUrl = !string.IsNullOrWhiteSpace(returnUrl)
                ? returnUrl
                : httpContext.Request.Headers.Referer.ToString();

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                targetUrl = "/";
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = targetUrl
            };

            properties.Items["prompt"] = "create";
            properties.Items["kc_action"] = "register";

            return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        app.MapGet("/bff/logout", (string? returnUrl, HttpContext httpContext) =>
        {
            var targetUrl = !string.IsNullOrWhiteSpace(returnUrl)
                ? returnUrl
                : httpContext.Request.Headers.Referer.ToString();

            if (string.IsNullOrWhiteSpace(targetUrl))
            {
                targetUrl = "/";
            }

            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = targetUrl },
                [
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme
                ]);
        });

        app.MapGet("/bff/user", async (ClaimsPrincipal user, HttpContext httpContext, ITokenRefreshService tokenService) =>
        {
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Ok(new UserInfoResponse(false, null, null, []));
            }

            // Proactively verify token validity and refresh if expired.
            // If Keycloak was restarted or refresh token is dead, this signs out the cookie and returns unauthenticated.
            var accessToken = await tokenService.GetOrRefreshAccessTokenAsync(httpContext);
            if (string.IsNullOrEmpty(accessToken))
            {
                return Results.Ok(new UserInfoResponse(false, null, null, []));
            }

            var claims = user.Claims.Select(c => new UserClaimDto(c.Type, c.Value)).ToList();
            var username = user.FindFirst("preferred_username")?.Value ?? user.FindFirst("name")?.Value ?? user.Identity.Name;
            var userId = user.FindFirst("sub")?.Value;

            return Results.Ok(new UserInfoResponse(true, userId, username, claims));
        });

        return app;
    }
}

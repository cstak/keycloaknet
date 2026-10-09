using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeycloakNet.WebBff.Services;

public class TokenRefreshService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TokenRefreshService> logger) : ITokenRefreshService
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public async Task<string?> GetOrRefreshAccessTokenAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var accessToken = await httpContext.GetTokenAsync("access_token");
        var expiresAtStr = await httpContext.GetTokenAsync("expires_at");

        bool isExpired = true;
        if (!string.IsNullOrEmpty(expiresAtStr) && DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
        {
            // Proactively refresh if fewer than 30 seconds remaining
            isExpired = expiresAt <= DateTimeOffset.UtcNow.AddSeconds(30);
        }
        else if (string.IsNullOrEmpty(accessToken))
        {
            isExpired = true;
        }

        if (!isExpired && !string.IsNullOrEmpty(accessToken))
        {
            return accessToken;
        }

        // Token is expired or expiring soon, acquire lock for thread-safe refresh
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check inside lock in case another concurrent request just refreshed the token
            accessToken = await httpContext.GetTokenAsync("access_token");
            expiresAtStr = await httpContext.GetTokenAsync("expires_at");
            if (!string.IsNullOrEmpty(expiresAtStr) &&
                DateTimeOffset.TryParse(expiresAtStr, out expiresAt) &&
                expiresAt > DateTimeOffset.UtcNow.AddSeconds(30) &&
                !string.IsNullOrEmpty(accessToken))
            {
                return accessToken;
            }

            var refreshToken = await httpContext.GetTokenAsync("refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
            {
                logger.LogWarning("Access token expired and no refresh token available in session cookie. Signing out.");
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            var authority = configuration["Authentication:Keycloak:Authority"]?.TrimEnd('/');
            if (string.IsNullOrEmpty(authority))
            {
                logger.LogError("Keycloak Authority is not configured in WebBff.");
                return null;
            }

            var tokenEndpoint = $"{authority}/protocol/openid-connect/token";
            var clientId = configuration["Authentication:Keycloak:ClientId"] ?? "";
            var clientSecret = configuration["Authentication:Keycloak:ClientSecret"] ?? "";

            var formParameters = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken
            };

            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(formParameters)
            };

            var response = await httpClient.SendAsync(refreshRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Keycloak rejected refresh token ({StatusCode}): {Error}. Invalidating stale session cookie.", response.StatusCode, errorBody);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                logger.LogWarning("Keycloak returned empty token payload on refresh. Signing out.");
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return null;
            }

            // Persist updated tokens to the active cookie session
            var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (authResult.Succeeded && authResult.Principal is not null)
            {
                var existingIdToken = authResult.Properties.GetTokenValue("id_token");
                var idTokenToKeep = tokenResponse.IdToken ?? existingIdToken;

                var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o");
                var updatedTokens = new List<AuthenticationToken>
                {
                    new() { Name = "access_token", Value = tokenResponse.AccessToken },
                    new() { Name = "refresh_token", Value = tokenResponse.RefreshToken ?? refreshToken },
                    new() { Name = "token_type", Value = tokenResponse.TokenType ?? "Bearer" },
                    new() { Name = "expires_at", Value = newExpiresAt }
                };

                if (!string.IsNullOrEmpty(idTokenToKeep))
                {
                    updatedTokens.Add(new() { Name = "id_token", Value = idTokenToKeep });
                }

                authResult.Properties.StoreTokens(updatedTokens);
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    authResult.Principal,
                    authResult.Properties);
            }

            logger.LogInformation("Successfully refreshed Keycloak access token. New validity: {ExpiresIn}s", tokenResponse.ExpiresIn);
            return tokenResponse.AccessToken;
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}

public class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }
}


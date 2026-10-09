using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KeycloakNet.KeycloakClient.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeycloakNet.KeycloakClient.Services;

public class KeycloakTokenService : IKeycloakTokenService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakClientOptions _options;
    private readonly ILogger<KeycloakTokenService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _cachedAccessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public KeycloakTokenService(
        HttpClient httpClient,
        IOptions<KeycloakClientOptions> options,
        ILogger<KeycloakTokenService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _cachedAccessToken;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _cachedAccessToken;
            }

            var tokenUrl = $"{_options.AuthServerUrl.TrimEnd('/')}/realms/{_options.AdminRealm}/protocol/openid-connect/token";

            var formFields = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(_options.ClientSecret))
            {
                formFields["grant_type"] = "client_credentials";
                formFields["client_id"] = _options.ClientId;
                formFields["client_secret"] = _options.ClientSecret;
            }
            else if (!string.IsNullOrEmpty(_options.AdminUsername))
            {
                formFields["grant_type"] = "password";
                formFields["client_id"] = _options.ClientId;
                formFields["username"] = _options.AdminUsername;
                formFields["password"] = _options.AdminPassword ?? string.Empty;
            }
            else
            {
                throw new InvalidOperationException("Keycloak client options must have either ClientSecret (client_credentials) or AdminUsername/AdminPassword (password grant) configured.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
            {
                Content = new FormUrlEncodedContent(formFields)
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to acquire Keycloak admin access token. Status: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Failed to acquire Keycloak admin access token: {response.StatusCode} - {errorBody}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Keycloak returned an empty token response.");
            }

            _cachedAccessToken = tokenResponse.AccessToken;
            // Refresh 30 seconds before expiration as a safety margin
            var validitySeconds = Math.Max(tokenResponse.ExpiresIn - 30, 30);
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(validitySeconds);

            _logger.LogDebug("Successfully acquired Keycloak admin access token. Valid for {Seconds} seconds.", validitySeconds);
            return _cachedAccessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private sealed class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 300;
    }
}


using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KeycloakNet.KeycloakClient.Abstractions;
using KeycloakNet.KeycloakClient.Models;
using KeycloakNet.KeycloakClient.Options;
using KeycloakNet.KeycloakClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KeycloakNet.KeycloakClient;

public class KeycloakClient : IKeycloakClient
{
    private readonly HttpClient _httpClient;
    private readonly IKeycloakTokenService _tokenService;
    private readonly KeycloakClientOptions _options;
    private readonly ILogger<KeycloakClient> _logger;

    public KeycloakClient(
        HttpClient httpClient,
        IKeycloakTokenService tokenService,
        IOptions<KeycloakClientOptions> options,
        ILogger<KeycloakClient> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
    }

    private string BaseAdminUrl => $"{_options.AuthServerUrl.TrimEnd('/')}/admin/realms/{_options.Realm}";

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetAdminAccessTokenAsync(cancellationToken);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/users";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, url, cancellationToken);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var locationHeader = response.Headers.Location;
            if (locationHeader != null)
            {
                var userId = locationHeader.Segments.Last().Trim('/');
                _logger.LogInformation("Successfully created Keycloak user '{Username}' with ID: {UserId}", request.Username, userId);
                return userId;
            }

            // Fallback: If Location header is missing, find user ID by username
            var user = await GetUserByUsernameAsync(request.Username, cancellationToken);
            return user?.Id ?? string.Empty;
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Keycloak user '{Username}' or email '{Email}' already exists (409 Conflict).", request.Username, request.Email);
            throw new InvalidOperationException($"User with username '{request.Username}' or email '{request.Email}' already exists in Keycloak.");
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to create Keycloak user. Status: {StatusCode}, Details: {Details}", response.StatusCode, error);
        throw new HttpRequestException($"Failed to create Keycloak user: {response.StatusCode} - {error}");
    }

    public async Task<KeycloakUserResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/users/{Uri.EscapeDataString(userId)}";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, url, cancellationToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KeycloakUserResponse>(cancellationToken: cancellationToken);
    }

    public async Task<KeycloakUserResponse?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/users?username={Uri.EscapeDataString(username)}&exact=true";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, url, cancellationToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserResponse>>(cancellationToken: cancellationToken);
        return users?.FirstOrDefault();
    }

    public async Task<KeycloakUserResponse?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/users?email={Uri.EscapeDataString(email)}&exact=true";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, url, cancellationToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserResponse>>(cancellationToken: cancellationToken);
        return users?.FirstOrDefault();
    }

    public async Task<List<KeycloakUserResponse>> GetUsersAsync(int max = 100, int first = 0, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/users?first={first}&max={max}";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, url, cancellationToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<KeycloakUserResponse>>(cancellationToken: cancellationToken);
        return users ?? [];
    }

    public async Task<bool> AssignRealmRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        // Retrieve role details
        var roleUrl = $"{BaseAdminUrl}/roles/{Uri.EscapeDataString(roleName)}";
        using var roleRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, roleUrl, cancellationToken);
        var roleResponse = await _httpClient.SendAsync(roleRequest, cancellationToken);

        if (!roleResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Keycloak role '{RoleName}' not found in realm '{Realm}'", roleName, _options.Realm);
            return false;
        }

        var role = await roleResponse.Content.ReadFromJsonAsync<KeycloakRoleDto>(cancellationToken: cancellationToken);
        if (role == null)
        {
            return false;
        }

        // Map role to the user

        var mappingUrl = $"{BaseAdminUrl}/users/{Uri.EscapeDataString(userId)}/role-mappings/realm";
        using var mapRequest = await CreateAuthorizedRequestAsync(HttpMethod.Post, mappingUrl, cancellationToken);
        mapRequest.Content = JsonContent.Create(new[] { role });

        var mapResponse = await _httpClient.SendAsync(mapRequest, cancellationToken);
        if (mapResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully assigned role '{RoleName}' to user '{UserId}'", roleName, userId);
            return true;
        }

        var error = await mapResponse.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to assign role '{RoleName}' to user '{UserId}': {Error}", roleName, userId, error);
        return false;
    }

    public async Task<bool> ResetPasswordAsync(string userId, string newPassword, bool temporary = false, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/users/{Uri.EscapeDataString(userId)}/reset-password";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Put, url, cancellationToken);

        var credential = new KeycloakCredentialDto
        {
            Type = "password",
            Value = newPassword,
            Temporary = temporary
        };

        httpRequest.Content = JsonContent.Create(credential);
        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully reset password for user '{UserId}'", userId);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to reset password for user '{UserId}': {Error}", userId, error);
        return false;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/users/{Uri.EscapeDataString(userId)}";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Delete, url, cancellationToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully deleted user '{UserId}'", userId);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Failed to delete user '{UserId}': {Error}", userId, error);
        return false;
    }

    public async Task<List<KeycloakRoleDto>> GetRealmRolesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseAdminUrl}/roles";
        using var httpRequest = await CreateAuthorizedRequestAsync(HttpMethod.Get, url, cancellationToken);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to retrieve realm roles for '{Realm}'. Status: {StatusCode}, Error: {Error}", _options.Realm, response.StatusCode, error);
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<KeycloakRoleDto>>(cancellationToken: cancellationToken) ?? [];
    }
}


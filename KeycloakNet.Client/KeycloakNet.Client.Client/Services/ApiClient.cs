using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace KeycloakNet.Client.Client.Services;

public class ApiClient(HttpClient httpClient)
{
    public async Task<UserInfoResponse?> GetUserInfoAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "bff/user");
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new UserInfoResponse(false, null, null, []);
            }

            return await response.Content.ReadFromJsonAsync<UserInfoResponse>();
        }
        catch
        {
            return new UserInfoResponse(false, null, null, []);
        }
    }

    public async Task<UserAboutResponse?> GetUserAboutAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "bff/api/user/about");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<UserAboutResponse>();
    }

    public async Task<MessageResponse?> GetPublicAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "bff/api/public");
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadFromJsonAsync<MessageResponse>();
    }

    public async Task<ArticleResponse?> CreateArticleAsync(string title, string content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "bff/api/articles")
        {
            Content = JsonContent.Create(new { title, content })
        };
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<ArticleResponse>();
    }

    // Admin / Headless User Management
    public async Task<List<AdminUserResponse>?> GetAdminUsersAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "bff/api/admin/users");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<List<AdminUserResponse>>();
    }

    public async Task<AdminUserResponse?> CreateAdminUserAsync(CreateAdminUserRequest createRequest)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "bff/api/admin/users")
        {
            Content = JsonContent.Create(createRequest)
        };
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<AdminUserResponse>();
    }

    public async Task<bool> DeleteAdminUserAsync(string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"bff/api/admin/users/{Uri.EscapeDataString(userId)}");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AdminRoleResponse>?> GetAdminRolesAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "bff/api/admin/roles");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<List<AdminRoleResponse>>();
    }
}

public record UserInfoResponse(bool IsAuthenticated, string? UserId, string? Username, List<UserClaimDto> Claims);
public record UserClaimDto(string Type, string Value);
public record UserAboutResponse(Guid UserId, string? Username, bool IsAuthenticated, List<string> Roles);
public record MessageResponse(string Message);
public record ArticleResponse(Guid Id, string Title, string Content, Guid AuthorId, DateTime CreatedAtUtc);

public record CreateAdminUserRequest(
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    string Password,
    List<string>? Roles = null
);

public record AdminUserResponse(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled,
    DateTimeOffset? CreatedTimestamp
);

public record AdminRoleResponse(
    string Id,
    string Name,
    string? Description
);

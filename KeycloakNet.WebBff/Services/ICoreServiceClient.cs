using System.Net.Http.Json;
using KeycloakNet.WebBff.Requests;
using KeycloakNet.WebBff.Responses;

namespace KeycloakNet.WebBff.Services;

public interface ICoreServiceClient
{
    Task<MessageResponse?> GetPublicAsync(CancellationToken cancellationToken = default);
    Task<MessageResponse?> GetProtectedAsync(CancellationToken cancellationToken = default);
    Task<UserAboutResponse?> GetUserAboutAsync(CancellationToken cancellationToken = default);
    Task<ArticleResponse?> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default);

    // Headless / Admin User Management via Keycloak REST API
    Task<List<AdminUserResponse>?> GetAdminUsersAsync(CancellationToken cancellationToken = default);
    Task<List<AdminRoleResponse>?> GetAdminRolesAsync(CancellationToken cancellationToken = default);
    Task<AdminUserResponse?> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAdminUserAsync(string id, CancellationToken cancellationToken = default);
}

public class CoreServiceClient(HttpClient httpClient) : ICoreServiceClient
{
    public async Task<MessageResponse?> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/public", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageResponse>(cancellationToken: cancellationToken);
    }

    public async Task<MessageResponse?> GetProtectedAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/protected", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageResponse>(cancellationToken: cancellationToken);
    }

    public async Task<UserAboutResponse?> GetUserAboutAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/user/about", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserAboutResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ArticleResponse?> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/articles", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ArticleResponse>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/articles/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AdminUserResponse>?> GetAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/admin/users", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<List<AdminUserResponse>>(cancellationToken: cancellationToken);
    }

    public async Task<List<AdminRoleResponse>?> GetAdminRolesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/admin/roles", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<List<AdminRoleResponse>>(cancellationToken: cancellationToken);
    }

    public async Task<AdminUserResponse?> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/admin/users", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}: {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAdminUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/admin/users/{Uri.EscapeDataString(id)}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

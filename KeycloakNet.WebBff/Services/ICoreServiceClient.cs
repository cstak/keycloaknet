using System.Net;
using System.Net.Http.Json;
using KeycloakNet.WebBff.Requests;
using KeycloakNet.WebBff.Responses;

namespace KeycloakNet.WebBff.Services;

public record DownstreamResult<T>(T? Data, HttpStatusCode StatusCode, string? Error = null)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

public record DownstreamResult(HttpStatusCode StatusCode, string? Error = null)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

public interface ICoreServiceClient
{
    Task<DownstreamResult<MessageResponse>> GetPublicAsync(CancellationToken cancellationToken = default);
    Task<DownstreamResult<MessageResponse>> GetProtectedAsync(CancellationToken cancellationToken = default);
    Task<DownstreamResult<UserAboutResponse>> GetUserAboutAsync(CancellationToken cancellationToken = default);
    Task<DownstreamResult<ArticleResponse>> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);
    Task<DownstreamResult> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default);

    // Headless / Admin User Management via Keycloak REST API
    Task<DownstreamResult<List<AdminUserResponse>>> GetAdminUsersAsync(CancellationToken cancellationToken = default);
    Task<DownstreamResult<List<AdminRoleResponse>>> GetAdminRolesAsync(CancellationToken cancellationToken = default);
    Task<DownstreamResult<AdminUserResponse>> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);
    Task<DownstreamResult> DeleteAdminUserAsync(string id, CancellationToken cancellationToken = default);
}

public class CoreServiceClient(HttpClient httpClient) : ICoreServiceClient
{
    public async Task<DownstreamResult<MessageResponse>> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/public", cancellationToken);
        return await ReadResultAsync<MessageResponse>(response, cancellationToken);
    }

    public async Task<DownstreamResult<MessageResponse>> GetProtectedAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/protected", cancellationToken);
        return await ReadResultAsync<MessageResponse>(response, cancellationToken);
    }

    public async Task<DownstreamResult<UserAboutResponse>> GetUserAboutAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/user/about", cancellationToken);
        return await ReadResultAsync<UserAboutResponse>(response, cancellationToken);
    }

    public async Task<DownstreamResult<ArticleResponse>> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/articles", request, cancellationToken);
        return await ReadResultAsync<ArticleResponse>(response, cancellationToken);
    }

    public async Task<DownstreamResult> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/articles/{id}", cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<DownstreamResult<List<AdminUserResponse>>> GetAdminUsersAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/admin/users", cancellationToken);
        return await ReadResultAsync<List<AdminUserResponse>>(response, cancellationToken);
    }

    public async Task<DownstreamResult<List<AdminRoleResponse>>> GetAdminRolesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/admin/roles", cancellationToken);
        return await ReadResultAsync<List<AdminRoleResponse>>(response, cancellationToken);
    }

    public async Task<DownstreamResult<AdminUserResponse>> CreateAdminUserAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/admin/users", request, cancellationToken);
        return await ReadResultAsync<AdminUserResponse>(response, cancellationToken);
    }

    public async Task<DownstreamResult> DeleteAdminUserAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/admin/users/{Uri.EscapeDataString(id)}", cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    private static async Task<DownstreamResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
            return new DownstreamResult<T>(data, response.StatusCode);
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return new DownstreamResult<T>(default, response.StatusCode, error);
    }

    private static async Task<DownstreamResult> ReadResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return new DownstreamResult(response.StatusCode);
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        return new DownstreamResult(response.StatusCode, error);
    }
}

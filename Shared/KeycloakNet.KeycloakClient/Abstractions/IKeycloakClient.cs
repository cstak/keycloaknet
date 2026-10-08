using KeycloakNet.KeycloakClient.Models;

namespace KeycloakNet.KeycloakClient.Abstractions;

public interface IKeycloakClient
{
    /// <summary>
    /// Creates a new user in Keycloak and returns the unique user ID (Guid string).
    /// </summary>
    Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their unique Keycloak ID.
    /// </summary>
    Task<KeycloakUserResponse?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for and retrieves a user by exact username.
    /// </summary>
    Task<KeycloakUserResponse?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for and retrieves a user by exact email address.
    /// </summary>
    Task<KeycloakUserResponse?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of users.
    /// </summary>
    Task<List<KeycloakUserResponse>> GetUsersAsync(int max = 100, int first = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a realm-level role to the specified user.
    /// </summary>
    Task<bool> AssignRealmRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets/updates the password of the specified user.
    /// </summary>
    Task<bool> ResetPasswordAsync(string userId, string newPassword, bool temporary = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified user from Keycloak.
    /// </summary>
    Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all realm-level roles defined in the Keycloak realm.
    /// </summary>
    Task<List<KeycloakRoleDto>> GetRealmRolesAsync(CancellationToken cancellationToken = default);
}


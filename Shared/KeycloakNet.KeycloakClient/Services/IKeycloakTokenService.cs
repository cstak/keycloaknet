namespace KeycloakNet.KeycloakClient.Services;

public interface IKeycloakTokenService
{
    Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken = default);
}


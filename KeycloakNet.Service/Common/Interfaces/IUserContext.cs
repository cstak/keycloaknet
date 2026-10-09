namespace KeycloakNet.Service.Common.Interfaces;

public interface IUserContext
{
    Guid UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}


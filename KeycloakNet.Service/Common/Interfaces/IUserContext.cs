namespace KeycloakNet.Service.Common.Interfaces;

public interface IUserContext
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}


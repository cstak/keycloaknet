namespace KeycloakNet.Service.Common.Exceptions;

public sealed class UserContextUnavailableException : Exception
{
    public UserContextUnavailableException() : base("User context is unavailable.")
    {
    }
}


namespace KeycloakNet.WebBff.Responses;

public record AdminUserResponse(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled,
    DateTimeOffset? CreatedTimestamp
);


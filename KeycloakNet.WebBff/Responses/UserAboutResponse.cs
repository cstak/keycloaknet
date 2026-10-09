namespace KeycloakNet.WebBff.Responses;

/// <summary>
/// Professional response model describing the authenticated user's profile and granted roles.
/// </summary>
public record UserAboutResponse(Guid UserId, string? Username, bool IsAuthenticated, List<string> Roles);


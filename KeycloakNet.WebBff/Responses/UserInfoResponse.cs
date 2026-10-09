using KeycloakNet.WebBff.DTOs;

namespace KeycloakNet.WebBff.Responses;

/// <summary>
/// Response model representing the current user's authentication status, identifiers, and claims.
/// </summary>
public record UserInfoResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Username,
    List<UserClaimDto> Claims
);


namespace KeycloakNet.WebBff.Responses;

/// <summary>
/// Professional response model representing an article entity returned from the core service.
/// </summary>
public record ArticleResponse(Guid Id, string Title, string Content, Guid AuthorId, DateTime CreatedAtUtc);


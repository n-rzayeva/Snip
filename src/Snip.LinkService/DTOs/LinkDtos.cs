namespace Snip.LinkService.DTOs;

public record CreateLinkRequest(string DestinationUrl);
public record UpdateLinkRequest(string DestinationUrl);

public record LinkResponse(
    Guid Id,
    string Slug,
    string DestinationUrl,
    DateTime CreatedAt,
    bool IsActive
);
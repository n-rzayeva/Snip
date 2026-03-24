namespace Snip.LinkService.DTOs;

public record CreateLinkRequest(string DestinationUrl);
public record UpdateLinkRequest(string DestinationUrl);
public record ClicksByHourDto(DateTime Hour, long Clicks);
public record ClicksByCountryDto(string Country, long Clicks);
public record ClicksByDeviceDto(string Device, long Clicks);

public record LinkResponse(
    Guid Id,
    string Slug,
    string DestinationUrl,
    DateTime CreatedAt,
    bool IsActive
);

public record LinkAnalyticsResponse(
    string Slug,
    long TotalClicks,
    IEnumerable<ClicksByHourDto> ClicksByHour,
    IEnumerable<ClicksByDeviceDto> ClicksByDevice
);
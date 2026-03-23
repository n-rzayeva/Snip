namespace Snip.Shared.Events;

public class ClickEvent
{
    public string Slug { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referer { get; set; }
}
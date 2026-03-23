namespace Snip.Shared.Events;

public class LinkUpdatedEvent
{
    public string Slug { get; set; } = string.Empty;
    public string NewDestinationUrl { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
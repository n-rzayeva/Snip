namespace Snip.Shared.Events;

public class LinkDeletedEvent
{
    public string Slug { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
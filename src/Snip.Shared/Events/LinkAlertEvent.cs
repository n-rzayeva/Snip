namespace Snip.Shared.Events;

public class LinkAlertEvent
{
    public string Slug { get; set; } = string.Empty;
    public long Milestone { get; set; }
    public long RealTotal { get; set; }
    public DateTime Timestamp { get; set; }
}
namespace Snip.Shared.Models;

public class Link
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DestinationUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
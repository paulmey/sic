namespace Sic.Core.Models;

public class Booking
{
    public string Id { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

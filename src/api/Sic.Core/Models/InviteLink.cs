namespace Sic.Core.Models;

public class InviteLink
{
    public string Id { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? UsedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

namespace Sic.Core.Models;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
    public string IdentityId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> AppRoles { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}

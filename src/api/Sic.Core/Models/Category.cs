namespace Sic.Core.Models;

public class Category
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

using Sic.Core.Models;
using Sic.Cosmos.Documents;

namespace Sic.Cosmos;

internal static class Mapper
{
    // User
    public static UserDocument ToDocument(User u) => new()
    {
        Id = u.Id, Pk = $"user:{u.Id}", Type = "user",
        IdentityProvider = u.IdentityProvider, IdentityId = u.IdentityId,
        DisplayName = u.DisplayName, AppRoles = u.AppRoles, CreatedAt = u.CreatedAt
    };
    public static User ToModel(UserDocument d) => new()
    {
        Id = d.Id, IdentityProvider = d.IdentityProvider, IdentityId = d.IdentityId,
        DisplayName = d.DisplayName, AppRoles = d.AppRoles, CreatedAt = d.CreatedAt
    };

    // Category
    public static CategoryDocument ToDocument(Category c) => new()
    {
        Id = c.Id, Pk = $"category:{c.Id}", Type = "category",
        Name = c.Name, Icon = c.Icon, CreatedAt = c.CreatedAt
    };
    public static Category ToModel(CategoryDocument d) => new()
    {
        Id = d.Id, Name = d.Name, Icon = d.Icon, CreatedAt = d.CreatedAt
    };

    // Resource
    public static ResourceDocument ToDocument(Resource r) => new()
    {
        Id = r.Id, Pk = $"resource:{r.Id}", Type = "resource",
        CategoryId = r.CategoryId, Name = r.Name, Description = r.Description,
        ImageUrl = r.ImageUrl, CreatedAt = r.CreatedAt
    };
    public static Resource ToModel(ResourceDocument d) => new()
    {
        Id = d.Id, CategoryId = d.CategoryId, Name = d.Name,
        Description = d.Description, ImageUrl = d.ImageUrl, CreatedAt = d.CreatedAt
    };

    // ResourceRole
    public static ResourceRoleDocument ToDocument(ResourceRole r) => new()
    {
        Id = r.Id, Pk = $"resource:{r.ResourceId}", Type = "resource-role",
        ResourceId = r.ResourceId, UserId = r.UserId, Role = r.Role
    };
    public static ResourceRole ToModel(ResourceRoleDocument d) => new()
    {
        Id = d.Id, ResourceId = d.ResourceId, UserId = d.UserId, Role = d.Role
    };

    // Booking
    public static BookingDocument ToDocument(Booking b) => new()
    {
        Id = b.Id, Pk = $"resource:{b.ResourceId}", Type = "booking",
        ResourceId = b.ResourceId, UserId = b.UserId, Title = b.Title,
        Description = b.Description, StartTime = b.StartTime, EndTime = b.EndTime,
        CreatedAt = b.CreatedAt
    };
    public static Booking ToModel(BookingDocument d) => new()
    {
        Id = d.Id, ResourceId = d.ResourceId, UserId = d.UserId, Title = d.Title,
        Description = d.Description, StartTime = d.StartTime, EndTime = d.EndTime,
        CreatedAt = d.CreatedAt
    };

    // InviteLink
    public static InviteLinkDocument ToDocument(InviteLink i) => new()
    {
        Id = i.Id, Pk = $"invite:{i.Id}", Type = "invite",
        CreatedByUserId = i.CreatedByUserId, ResourceId = i.ResourceId,
        ExpiresAt = i.ExpiresAt, UsedByUserId = i.UsedByUserId, CreatedAt = i.CreatedAt
    };
    public static InviteLink ToModel(InviteLinkDocument d) => new()
    {
        Id = d.Id, CreatedByUserId = d.CreatedByUserId, ResourceId = d.ResourceId,
        ExpiresAt = d.ExpiresAt, UsedByUserId = d.UsedByUserId, CreatedAt = d.CreatedAt
    };
}

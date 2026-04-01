namespace Sic.Core;

public static class AppRoles
{
    public const string UserAdmin = "user-admin";
    public const string CategoryAdmin = "category-admin";
    public const string ResourceAdmin = "resource-admin";

    public static readonly string[] All = { UserAdmin, CategoryAdmin, ResourceAdmin };
}

public static class ResourceRoles
{
    public const string User = "user";
    public const string Manager = "manager";
}

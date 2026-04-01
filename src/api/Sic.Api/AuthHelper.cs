using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sic.Api;

public static class AuthHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ClientPrincipal? GetClientPrincipal(HttpRequest req)
    {
        var header = req.Headers["x-ms-client-principal"].FirstOrDefault();
        if (string.IsNullOrEmpty(header))
            return null;

        try
        {
            var decoded = Convert.FromBase64String(header);
            var json = Encoding.UTF8.GetString(decoded);
            return JsonSerializer.Deserialize<ClientPrincipal>(json, JsonOptions);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public class ClientPrincipal
{
    public string IdentityProvider { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserDetails { get; set; } = string.Empty;
    public IEnumerable<string> UserRoles { get; set; } = Array.Empty<string>();
}

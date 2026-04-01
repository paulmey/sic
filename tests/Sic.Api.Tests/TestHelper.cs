using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sic.Api.Tests;

public static class TestHelper
{
    public static HttpRequest CreateRequest(
        string identityProvider = "microsoft",
        string userId = "user-1",
        string userDetails = "test@example.com",
        object? body = null,
        Dictionary<string, string>? queryParams = null)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;

        var principal = new
        {
            identityProvider,
            userId,
            userDetails,
            userRoles = new[] { "authenticated" }
        };
        var json = JsonSerializer.Serialize(principal);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        request.Headers["x-ms-client-principal"] = base64;

        if (body is not null)
        {
            var bodyJson = JsonSerializer.Serialize(body);
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson));
            request.ContentType = "application/json";
        }

        if (queryParams is not null)
        {
            var queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            request.QueryString = new QueryString("?" + queryString);
        }

        return request;
    }

    public static HttpRequest CreateAnonymousRequest()
    {
        var context = new DefaultHttpContext();
        return context.Request;
    }
}

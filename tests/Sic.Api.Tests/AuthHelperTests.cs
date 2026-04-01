using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sic.Api.Tests;

public class AuthHelperTests
{
    [Fact]
    public void GetClientPrincipal_WithValidHeader_ReturnsPrincipal()
    {
        var req = TestHelper.CreateRequest(identityProvider: "google", userId: "g-123", userDetails: "test@gmail.com");

        var principal = AuthHelper.GetClientPrincipal(req);

        Assert.NotNull(principal);
        Assert.Equal("google", principal!.IdentityProvider);
        Assert.Equal("g-123", principal.UserId);
        Assert.Equal("test@gmail.com", principal.UserDetails);
    }

    [Fact]
    public void GetClientPrincipal_WithNoHeader_ReturnsNull()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var principal = AuthHelper.GetClientPrincipal(req);

        Assert.Null(principal);
    }

    [Fact]
    public void GetClientPrincipal_WithMalformedBase64_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-ms-client-principal"] = "not-valid-base64!!!";

        var principal = AuthHelper.GetClientPrincipal(context.Request);

        Assert.Null(principal);
    }

    [Fact]
    public void GetClientPrincipal_WithEmptyHeader_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["x-ms-client-principal"] = "";

        var principal = AuthHelper.GetClientPrincipal(context.Request);

        Assert.Null(principal);
    }
}

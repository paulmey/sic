using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Api.Functions;

public class UserFunctions
{
    private readonly UserService _userService;
    private readonly IUserRepository _userRepo;

    public UserFunctions(UserService userService, IUserRepository userRepo)
    {
        _userService = userService;
        _userRepo = userRepo;
    }

    [Function("GetMe")]
    public async Task<IActionResult> GetMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var result = await _userService.AuthenticateOrCreateAsync(
            principal.IdentityProvider, principal.UserId, principal.UserDetails);

        if (!result.Success)
            return new BadRequestObjectResult(new { error = result.Error });

        return new OkObjectResult(result.Value);
    }

    [Function("UpdateMe")]
    public async Task<IActionResult> UpdateMe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "me")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var body = await JsonSerializer.DeserializeAsync<UpdateProfileRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (body is null || string.IsNullOrWhiteSpace(body.DisplayName))
            return new BadRequestObjectResult(new { error = "DisplayName is required." });

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null)
            return new NotFoundResult();

        user.DisplayName = body.DisplayName;
        await _userRepo.UpdateAsync(user);

        return new OkObjectResult(user);
    }
}

public record UpdateProfileRequest(string? DisplayName);

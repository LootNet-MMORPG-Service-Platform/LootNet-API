namespace LootNet_API.Extensions;

using System.Security.Claims;

public static class UserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();

        return Guid.Parse(id);
    }
}
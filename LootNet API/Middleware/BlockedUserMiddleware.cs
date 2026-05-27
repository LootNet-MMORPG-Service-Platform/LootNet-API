namespace LootNet_API.Middleware;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LootNet_API.Data;
using Microsoft.EntityFrameworkCore;

public class BlockedUserMiddleware
{
    private static readonly PathString[] ExcludedPaths =
    {
        new("/api/auth/login"),
        new("/api/auth/register"),
        new("/api/auth/refresh"),
        new("/api/auth/logout"),
        new("/api/auth/verify-email"),
        new("/api/auth/resend-verification"),
        new("/api/auth/forgot-password"),
        new("/api/auth/reset-password-email")
    };

    private readonly RequestDelegate _next;

    public BlockedUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true && !IsExcluded(context.Request.Path))
        {
            var userId = TryGetUserId(context.User);
            if (userId.HasValue)
            {
                var blocked = await db.Users
                    .AnyAsync(x => x.Id == userId.Value && x.IsBlocked);

                if (blocked)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Account is blocked.");
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsExcluded(PathString path)
        => ExcludedPaths.Any(x => path.StartsWithSegments(x));

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}

using System.Security.Claims;
using backend.Data;
using backend.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend.Middleware;

public class UserStatusCheckMiddleware
{
    private readonly RequestDelegate _next;

    public UserStatusCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user == null)
                {
                    // User deleted
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { message = "Your account has been deleted." });
                    return;
                }
                
                if (user.Status == UserStatus.Blocked)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { message = "Your account has been blocked." });
                    return;
                }
                
                if (user.Status == UserStatus.Unverified)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { message = "Your account is unverified." });
                    return;
                }
            }
        }

        await _next(context);
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class UserStatusCheckMiddlewareExtensions
{
    public static IApplicationBuilder UseUserStatusCheck(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserStatusCheckMiddleware>();
    }
}

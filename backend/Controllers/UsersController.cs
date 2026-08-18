using backend.Data;
using backend.Enums;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var currentUserIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid.TryParse(currentUserIdString, out var currentUserId);

        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Status,
                u.Verified,
                u.LastSeen,
                IsCurrentUser = u.Id == currentUserId
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("block")]
    public async Task<IActionResult> BlockUsers([FromBody] List<Guid> userIds)
    {
        return await ProcessUsersActionAsync(userIds, users =>
        {
            users.ForEach(u => u.Status = UserStatus.Blocked);
        }, "users blocked");
    }

    [HttpPut("unblock")]
    public async Task<IActionResult> UnblockUsers([FromBody] List<Guid> userIds)
    {
        return await ProcessUsersActionAsync(userIds, users =>
        {
            users.ForEach(u => u.Status = UserStatus.Active);
        }, "users unblocked/verified");
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteUsers([FromBody] List<Guid> userIds)
    {
        return await ProcessUsersActionAsync(userIds, users =>
        {
            _context.Users.RemoveRange(users);
        }, "users deleted");
    }

    private async Task<IActionResult> ProcessUsersActionAsync(List<Guid> userIds, Action<List<User>> action, string successMessageSuffix)
    {
        if (userIds == null || !userIds.Any()) return BadRequest("No users specified");

        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        if (users.Any())
        {
            action(users);
            await _context.SaveChangesAsync();
        }
        
        return Ok(new { message = $"{users.Count} {successMessageSuffix}" });
    }
}

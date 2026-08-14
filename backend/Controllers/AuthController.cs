using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.Enums;
using backend.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Name, Email and Password are required." });
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Status = UserStatus.Unverified, // Default as requested
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Assuming the exception is due to unique constraint on Email
            return BadRequest(new { message = "A user with this email already exists." });
        }

        return Ok(new { message = "Registration successful. Please wait for an active user to verify your account." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Email and Password are required." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (user.Status == UserStatus.Blocked)
        {
            return StatusCode(403, new { message = "Your account has been blocked." });
        }
        
        if (user.Status == UserStatus.Unverified)
        {
            return StatusCode(403, new { message = "Your account is unverified. Please wait for an active user to unlock it." });
        }

        // Update LastSeen
        user.LastSeen = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new
        {
            token,
            user = new { user.Id, user.Name, user.Email, user.Status }
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSecret = Env.GetString("JWT_SECRET") ?? "fallback_secret_for_development_purposes_only_which_is_long";
        var key = Encoding.ASCII.GetBytes(jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}

public class RegisterDto
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

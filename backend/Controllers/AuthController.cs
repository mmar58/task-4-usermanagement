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
    private readonly backend.Services.IEmailService _emailService;

    public AuthController(AppDbContext context, backend.Services.IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || dto.Password == null || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Name and Email are required." });
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Status = UserStatus.Active,
            Verified = 0,
            VerificationToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
            await _emailService.SendVerificationEmailAsync(user.Email, user.Name, user.VerificationToken);
        }
        catch (DbUpdateException ex)
        {
            // Assuming the exception is due to unique constraint on Email
            return BadRequest(new { message = "A user with this email already exists." });
        }

        return Ok(new { message = "Registration successful. Please check your email to verify your account." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || dto.Password == null)
        {
            return BadRequest(new { message = "Email is required." });
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

        // Update LastSeen
        user.LastSeen = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new
        {
            token,
            user = new { user.Id, user.Name, user.Email, user.Status, user.Verified }
        });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            return BadRequest(new { message = "Verification token is required." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == dto.Token);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid or expired verification token." });
        }

        user.Verified = 1;
        user.VerificationToken = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Email verified successfully.", name = user.Name });
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

public class VerifyDto
{
    public required string Token { get; set; }
}

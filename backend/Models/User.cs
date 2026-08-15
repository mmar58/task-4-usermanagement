using backend.Enums;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public int Verified { get; set; } = 0;
    public string? VerificationToken { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}

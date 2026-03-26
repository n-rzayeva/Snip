using Microsoft.AspNetCore.Identity;

namespace Snip.AuthService.Models;

public class AppUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
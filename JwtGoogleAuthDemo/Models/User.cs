namespace JwtGoogleAuthDemo.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string? PasswordHash { get; set; } // Null for OAuth users
    public string? Name { get; set; }
    public string? Provider { get; set; } // "local", "Google", etc.
    public string? ProviderUserId { get; set; } // For OAuth users
}


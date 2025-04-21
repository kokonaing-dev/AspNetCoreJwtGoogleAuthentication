using JwtGoogleAuthDemo.Models;

namespace JwtGoogleAuthDemo.Services;

public interface IJwtService
{
    string GenerateJwtToken(User user);
}

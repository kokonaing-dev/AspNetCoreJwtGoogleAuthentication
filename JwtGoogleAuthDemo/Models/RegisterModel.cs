using System.ComponentModel.DataAnnotations;

namespace JwtGoogleAuthDemo.Models;

public class RegisterModel
{
    [Required][EmailAddress] 
    public string Email { get; set; }

    [Required] 
    public string Name { get; set; }

    [Required][StringLength(100, MinimumLength = 6)] 
    public string Password { get; set; }

    [Required] 
    public string ConfirmPassword { get; set; }
}

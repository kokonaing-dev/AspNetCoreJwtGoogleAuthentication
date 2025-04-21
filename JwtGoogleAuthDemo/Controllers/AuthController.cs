using JwtGoogleAuthDemo;
using JwtGoogleAuthDemo.Models;
using JwtGoogleAuthDemo.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class AuthController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;

    public AuthController(AppDbContext context, IPasswordHasherService passwordHasher, IConfiguration configuration, IJwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _jwtService = jwtService;
    }

    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.Provider == "Local");

            if (user != null &&
                _passwordHasher.VerifyPassword(user.PasswordHash, model.Password))
            {
                // Create claims identity for MVC auth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name ?? user.Email),
                    new Claim("provider", user.Provider)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                var token = _jwtService.GenerateJwtToken(user);

                Response.Cookies.Append("jwt_token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddHours(1)
                });

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt");
        }

        return View(model);
    }

    // GET: /Auth/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            var user = new User
            {
                Email = model.Email,
                Name = model.Name,
                Provider = "Local",
                PasswordHash = _passwordHasher.HashPassword(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login", "Auth");
        }

        return View(model);
    }

    
    // Google login redirect and callback
    [HttpGet]
    public IActionResult LoginWithGoogle()
    {
        var redirectUrl = Url.Action("GoogleCallback", "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return Unauthorized("External authentication failed.");

        var claimsIdentity = result.Principal.Identity as ClaimsIdentity;
        var email = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;
        var name = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
            return BadRequest("Google account did not provide an email.");

        // Check if user exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                Name = name ?? email,
                Provider = "Google",
                PasswordHash = null // optional: or "GoogleLogin" or leave blank
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        var token = _jwtService.GenerateJwtToken(user);

        // Store JWT in a cookie
        Response.Cookies.Append("jwt_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return RedirectToAction("Index", "Home");
    }


    [HttpGet]
    public IActionResult Logout()
    {
        // Sign out of the cookie authentication scheme
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Remove the JWT cookie
        Response.Cookies.Delete("jwt_token");

        return RedirectToAction("Login", "Auth");
    }


    // GET: /Auth/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtGoogleAuthDemo.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult Privacy()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("AccessDenied", "Auth");
        }

        var jwtToken = Request.Cookies["jwt_token"];
        ViewData["JwtToken"] = jwtToken;

        return View();
    }
}

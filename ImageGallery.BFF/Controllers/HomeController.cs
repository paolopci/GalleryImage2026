using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.BFF.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["IsAuthenticated"] = User.Identity?.IsAuthenticated ?? false;
        ViewData["DisplayName"] = User.Claims.FirstOrDefault(claim => claim.Type == "given_name")?.Value
            ?? User.Identity?.Name
            ?? "utente anonimo";

        return View();
    }
}

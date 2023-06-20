using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthCode.Dotnet.HighLevel.Controllers;

public class ProtectedController : Controller
{
    // GET
    [Authorize]
    public IActionResult Index()
    {
        return View();
    }
}
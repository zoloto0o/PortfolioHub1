using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PortfolioHub.Controllers;

[Authorize(Roles = "Admin,Moderator")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace WebMobileAssignment.Controllers;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace WebMobileAssignment.Controllers;

public class HomeController(DB db) : Controller
{
   public IActionResult Index()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Management", "Anime");
        }

        return RedirectToAction("Index", "Anime");
    }
}

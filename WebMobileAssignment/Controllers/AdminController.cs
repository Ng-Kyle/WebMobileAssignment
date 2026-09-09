using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WebMobileAssignment.Models;
using X.PagedList;
using X.PagedList.Extensions;

namespace WebMobileAssignment.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(DB db, Helper hp, IStringLocalizer<SharedResource> localizer) : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Management", "Anime");
    }

    public IActionResult Admins()
    {
        ViewBag.AdminListing = "admin_sidenav-active";
        ViewBag.Admins = db.Users.OfType<Admin>().ToList();
        return View(new RegisterAdminVM());
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAdmin(RegisterAdminVM vm)
    {
        var existingUser = db.Users.FirstOrDefault(u => u.Email == vm.Email);
        if (existingUser != null)
        {
            if (existingUser.Role == "Member")
            {
                ModelState.AddModelError("Email", localizer["This email belongs to a Member (cannot promote to Admin)."]);
            }
            else
            {
                ModelState.AddModelError("Email", localizer["Email is already in use by an Admin."]);
            }
        }

        if (db.Users.Any(u => u.UserName == vm.UserName))
        {
            ModelState.AddModelError("UserName", localizer["Username is already taken."]);
        }

        if (ModelState.IsValid)
        {
            var admin = new Admin
            {
                UserName = vm.UserName.Trim(),
                Email = vm.Email.Trim(),
                PasswordHash = hp.HashPassword(vm.Password)
            };

            db.Add(admin);
            await db.SaveChangesAsync();

            TempData["Info"] = localizer["Admin account '{0}' created successfully.", admin.UserName].Value;
            return RedirectToAction(nameof(Admins));
        }

        ViewBag.AdminListing = "admin_sidenav-active";
        ViewBag.Admins = db.Users.OfType<Admin>().ToList();
        return View("Admins", vm);
    }

    [HttpGet]
    public IActionResult CheckEmail(string email)
    {
        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user != null)
        {
            string message = user.Role == "Member"
                ? localizer["This email belongs to a Member (cannot promote to Admin)."]
                : localizer["Email is already in use by an Admin."];

            return Json(new { exists = true, message });
        }

        return Json(new { exists = false });
    }

    [HttpGet]
    public IActionResult CheckUserName(string userName)
    {
        bool exists = db.Users.Any(u => u.UserName == userName);
        return Json(new { exists, message = exists ? localizer["Username is already taken."] : "" });
    }

    [HttpPost]
    public IActionResult DeleteAdmin(string email)
    {
        if (email.Equals(User.Identity!.Name, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Info"] = localizer["Action denied: You cannot delete your own admin account."].Value;
            return RedirectToAction(nameof(Admins));
        }
        var target = db.Users.OfType<Admin>().FirstOrDefault(u => u.Email == email);
        if (target != null)
        {
            db.Users.Remove(target);
            db.SaveChanges();
            TempData["Info"] = localizer["Admin '{0}' has been deleted.", target.UserName].Value;
        }

        return RedirectToAction(nameof(Admins));
    }

    public async Task<IActionResult> Reviews(string search, int? rating, int? animeId, string sort = "Date", string direction = "des", int page = 1)
    {
        var query = db.Reviews
            .Include(r => r.Member)
            .Include(r => r.Anime)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var clean = search.Trim();
            query = query.Where(r => r.Text.Contains(clean) || r.Member.UserName.Contains(clean));
            ViewBag.Search = clean;
        }

        if (rating.HasValue && rating > 0)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (animeId.HasValue && animeId > 0)
        {
            query = query.Where(r => r.Anime.AnimeId == animeId.Value);
        }

        query = (sort, direction) switch
        {
            ("Rating", "asc") => query.OrderBy(r => r.Rating),
            ("Rating", "des") => query.OrderByDescending(r => r.Rating),
            ("Date", "asc") => query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        ViewBag.AnimeList = new SelectList(await db.Animes.OrderBy(a => a.Name).ToListAsync(), "AnimeId", "Name");

        var vm = new AdminReviewIndexVM
        {
            Reviews = query.ToPagedList(page, 10),
            Search = search,
            SelectedRating = rating,
            SelectedAnimeId = animeId,
            Sort = sort,
            Direction = direction
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var review = await db.Reviews.FindAsync(reviewId);
        if (review != null)
        {
            db.Reviews.Remove(review);
            await db.SaveChangesAsync();
            TempData["Info"] = localizer["Review has been deleted by administrator."].Value;
        }
        return RedirectToAction(nameof(Reviews));
    }

    public async Task<IActionResult> ReviewDetails(int id)
    {
        var review = await db.Reviews
            .Include(r => r.Member)
            .Include(r => r.Anime)
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (review == null)
        {
            return NotFound();
        }

        return View(review);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebMobileAssignment.Models;
using X.PagedList.Extensions;

namespace WebMobileAssignment.Controllers;

public class AdminController(DB db) : Controller
{
    // GET: /Admin
    public IActionResult Index()
    {
        return RedirectToAction("Management", "Anime");
    }

    // GET: /Admin/Reviews
    public async Task<IActionResult> Reviews(string? search, int? rating, int? animeId, string sort = "Date", string direction = "des", int page = 1)
    {
        var query = db.Reviews
            .Include(r => r.Member)
            .Include(r => r.Anime)
            .AsQueryable();

        // Search by review text or member username
        if (!string.IsNullOrWhiteSpace(search))
        {
            var clean = search.Trim();
            query = query.Where(r => r.Text.Contains(clean) || (r.Member != null && r.Member.UserName.Contains(clean)));
            ViewBag.Search = clean;
        }

        // Filter by Rating
        if (rating.HasValue && rating > 0)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        // Filter by Anime
        if (animeId.HasValue && animeId > 0)
        {
            query = query.Where(r => r.Anime != null && r.Anime.AnimeId == animeId.Value);
        }

        // Sorting
        query = (sort, direction) switch
        {
            ("Rating", "asc") => query.OrderBy(r => r.Rating),
            ("Rating", "des") => query.OrderByDescending(r => r.Rating),
            ("Date", "asc") => query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        ViewBag.AnimeList = new SelectList(await db.Animes.OrderBy(a => a.Name).ToListAsync(), "AnimeId", "Name");

        var list = await query.ToListAsync();
        var pagedReviews = list.ToPagedList(page, 10);

        var vm = new AdminReviewIndexVM
        {
            Reviews = pagedReviews,
            Search = search,
            SelectedRating = rating,
            SelectedAnimeId = animeId,
            Sort = sort,
            Direction = direction
        };

        return View(vm);
    }

    // POST: /Admin/DeleteReview
    [HttpPost]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var review = await db.Reviews.FindAsync(reviewId);
        if (review != null)
        {
            db.Reviews.Remove(review);
            await db.SaveChangesAsync();
            TempData["Info"] = "Review has been deleted by administrator.";
        }
        return RedirectToAction(nameof(Reviews));
    }

    // GET: /Admin/ReviewDetails/5
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
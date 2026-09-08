using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using X.PagedList.Extensions;

namespace WebMobileAssignment.Controllers;

public class MemberController (DB db, Helper hp, IStringLocalizer<SharedResource> localizer): Controller
{
    public IActionResult Management(string? name, string? sort, string? direction, int page = 1)
    {
        ViewBag.Name = name = name?.Trim() ?? "";
        var searched = db.Members.Include(a => a.Reviews).Include(a=>a.WatchList).ThenInclude(w => w.AnimeWatchLists).Where(a => a.UserName.Contains(name) || a.Email.Contains(name));

        ViewBag.Sort = sort;
        ViewBag.Direction = direction;

        Func<Member, object> fSort = sort switch
        {
            "UserName" => member => member.UserName,
            "Email" => member => member.Email,
            "Animes in Watchlist" => member => member.WatchList.AnimeWatchLists.Count,
            "Reviews Written" => member => member.Reviews.Count,
            "Account Status" => member => member.IsBlocked,
            _ => member => member.UserName
        };
        var sorted = direction == "des" ? searched.OrderByDescending(fSort) : searched.OrderBy(fSort);

        if (page < 1)
        {
            return RedirectToAction(null, new { name, sort, direction, page = 1 });
        }

        var a = sorted.ToPagedList(page, 10);
        if (page > a.PageCount && a.PageCount > 0)
        {
            return RedirectToAction(null, new { name, sort, direction, page = a.PageCount });
        }
        if (Request.IsAjax())
        {
            return PartialView("AdminMemberTable", a);
        }
        return View(a);
    }
    [HttpPost]
    public IActionResult Block(int userId)
    {
        var user = db.Members.Find(userId);
        if(user == null)
        {
            TempData["Info"] = localizer["No member found!"].Value;
            return RedirectToAction("Management");
        }
        user.IsBlocked = true;
        db.SaveChanges();
        TempData["Info"] = localizer["MemberBlocked", user.UserName].Value;
        return RedirectToAction("Management");
    }
    [HttpPost]
    public IActionResult Unblock(int userId)
    {
        var user = db.Members.Find(userId);
        if (user == null)
        {
            TempData["Info"] = localizer["No member found!"].Value;
            return RedirectToAction("Management");
        }
        user.IsBlocked = false;
        db.SaveChanges();
        TempData["Info"] = localizer["MemberUnblocked",user.UserName].Value;
        return RedirectToAction("Management");
    }
    public IActionResult ManagementDetails(int userId)
    {
        var user = db.Members.Include(m => m.Reviews).Include(m => m.WatchList).ThenInclude(m => m.AnimeWatchLists).ThenInclude(m=> m.Anime).ThenInclude(m => m.AnimeImages).FirstOrDefault(m => m.UserId == userId);
        if (user == null)
        {
            return RedirectToAction("Management");
        }
        return View(user);
    }

    public async Task<IActionResult> ProgressStats(int userId)
    {
        // Fetch user's watchlist items with attached Anime details
        var userWatchlist = await db.AnimeWatchLists
            .Include(aw => aw.Anime)
            .Where(aw => aw.WatchList.Member.UserId == userId)
            .ToListAsync();

        int totalAnimes = userWatchlist.Count;
        int completedCount = userWatchlist.Count(aw => aw.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));

        var vm = new ProgressStatsVM
        {
            TotalAnimesInList = totalAnimes,
            TotalEpisodesWatched = userWatchlist.Sum(aw => aw.Progress),
            CompletionRatePercentage = totalAnimes > 0
                ? Math.Round((double)completedCount / totalAnimes * 100, 1)
                : 0.0,

            WatchingCount = userWatchlist.Count(aw => aw.Status.Equals("Watching", StringComparison.OrdinalIgnoreCase)),
            CompletedCount = completedCount,
            PlanToWatchCount = userWatchlist.Count(aw => aw.Status.Equals("Plan to Watch", StringComparison.OrdinalIgnoreCase)),
            OnHoldCount = userWatchlist.Count(aw => aw.Status.Equals("On Hold", StringComparison.OrdinalIgnoreCase)),
            DroppedCount = userWatchlist.Count(aw => aw.Status.Equals("Dropped", StringComparison.OrdinalIgnoreCase)),

            RecentlyCompleted = userWatchlist
                .Where(aw => aw.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) && aw.EndDate.HasValue)
                .OrderByDescending(aw => aw.EndDate)
                .Take(5)
                .ToList()
        };

        return View(vm);
    }

    public async Task<IActionResult> RatingStats(int userId)
    {
        // Fetch member's reviews with associated Anime data
        var memberReviews = await db.Reviews
            .Include(r => r.Anime)
            .Where(r => r.Member.UserId == userId)
            .ToListAsync();

        int totalReviews = memberReviews.Count;

        var vm = new RatingStatsVM
        {
            TotalReviewsCount = totalReviews,
            AverageRating = totalReviews > 0 ? Math.Round(memberReviews.Average(r => r.Rating), 2) : 0.0,
            HighestRating = totalReviews > 0 ? memberReviews.Max(r => r.Rating) : 0,
            LowestRating = totalReviews > 0 ? memberReviews.Min(r => r.Rating) : 0,

            TopRatedReviews = memberReviews
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList()
        };

        // Populate score breakdown from 1 to 10
        for (int score = 1; score <= 10; score++)
        {
            vm.RatingDistribution[score] = memberReviews.Count(r => r.Rating == score);
        }

        return View(vm);
    }

    public async Task<IActionResult> PreferencesStats(int userId)
    {
        // Load watchlist items along with related WatchList.Member, Anime, Genres, Studios, and Reviews
        var watchlistItems = await db.AnimeWatchLists
            .Include(w => w.WatchList)
                .ThenInclude(wl => wl.Member)
            .Include(w => w.Anime)
                .ThenInclude(a => a.Genres)
            .Include(w => w.Anime)
                .ThenInclude(a => a.Studio)
            .Include(w => w.Anime)
                .ThenInclude(a => a.Reviews)
                    .ThenInclude(r => r.Member)
            .Where(w => w.WatchList.Member.UserId == userId)
            .ToListAsync();

        // Group and rank by Genre
        var genreStats = watchlistItems
            .SelectMany(w => w.Anime.Genres.Select(g => new { Genre = g.Name, Anime = w.Anime }))
            .GroupBy(x => x.Genre)
            .Select(g => new PreferenceItemVM
            {
                Name = g.Key,
                Count = g.Count(),
                AverageRating = g.SelectMany(x => x.Anime.Reviews.Where(r => r.Member.UserId == userId))
                                 .Select(r => (double?)r.Rating)
                                 .Average() ?? 0.0
            })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.AverageRating)
            .Take(5)
            .ToList();

        // Group and rank by Studio
        var studioStats = watchlistItems
            .Where(w => w.Anime.Studio != null)
            .GroupBy(w => w.Anime.Studio!.Name)
            .Select(g => new PreferenceItemVM
            {
                Name = g.Key,
                Count = g.Count(),
                AverageRating = g.SelectMany(w => w.Anime.Reviews.Where(r => r.Member.UserId == userId))
                                 .Select(r => (double?)r.Rating)
                                 .Average() ?? 0.0
            })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.AverageRating)
            .Take(5)
            .ToList();

        var vm = new PreferencesStatsVM
        {
            TopGenres = genreStats,
            TopStudios = studioStats
        };

        return View(vm);
    }

    // POST: Create or Update Member Review + Rating
    [HttpPost]
    public async Task<IActionResult> SaveReview(MemberReviewVM vm)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1; // Adapt to your Auth system

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Invalid review payload.";
            return RedirectToAction("Details", "Anime", new { id = vm.AnimeId });
        }

        var existingReview = await db.Reviews
            .FirstOrDefaultAsync(r => r.Anime.AnimeId == vm.AnimeId && r.Member.UserId == userId);

        if (existingReview != null)
        {
            // Update Review
            existingReview.Rating = vm.Rating;
            existingReview.Text = vm.Text;
            existingReview.UpdatedAt = DateTime.Now;
            db.Reviews.Update(existingReview);
            TempData["Info"] = "Your review has been updated!";
        }
        else
        {
            // Create Review
            var member = await db.Members.FirstAsync(m => m.UserId == userId);
            var anime = await db.Animes.FirstAsync(a => a.AnimeId == vm.AnimeId);

            var newReview = new Review
            {
                Rating = vm.Rating,
                Text = vm.Text,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Member = member,
                Anime = anime
            };
            db.Reviews.Add(newReview);
            TempData["Info"] = "Your review has been published!";
        }

        await db.SaveChangesAsync();
        return RedirectToAction("Details", "Anime", new { id = vm.AnimeId });
    }

    // POST: Member Delete Review
    [HttpPost]
    public async Task<IActionResult> DeleteMyReview(int reviewId)
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 1;
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.Member.UserId == userId);

        if (review != null)
        {
            db.Reviews.Remove(review);
            await db.SaveChangesAsync();
            TempData["Info"] = "Review deleted successfully.";
        }
        return RedirectToAction("Management", "Member");
    }
}



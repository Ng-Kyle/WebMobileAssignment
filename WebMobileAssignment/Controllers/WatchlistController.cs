using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMobileAssignment.Models;

namespace WebMobileAssignment.Controllers;

public class WatchlistController(DB db) : Controller
{
    private int GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId") ?? 1;
    }

    // GET: /Watchlist
    public async Task<IActionResult> Index(string? search, string? status)
    {
        int userId = GetCurrentUserId();

        var query = db.AnimeWatchLists
            .Include(aw => aw.Anime).ThenInclude(a => a.AnimeImages)
            .Include(aw => aw.Anime).ThenInclude(a => a.Studio)
            .Include(aw => aw.WatchList)
            .Where(aw => aw.WatchList.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(aw => aw.Anime.Name.Contains(search.Trim()));
            ViewBag.Search = search.Trim();
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(aw => aw.Status == status);
            ViewBag.SelectedStatus = status;
        }
        else
        {
            ViewBag.SelectedStatus = "All";
        }

        var allItems = await db.AnimeWatchLists
            .Where(aw => aw.WatchList.UserId == userId)
            .ToListAsync();

        // Populate dashboard count metrics
        ViewBag.TotalCount = allItems.Count;
        ViewBag.WatchingCount = allItems.Count(aw => aw.Status == "Watching");
        ViewBag.CompletedCount = allItems.Count(aw => aw.Status == "Completed");
        ViewBag.PlanCount = allItems.Count(aw => aw.Status == "Plan to Watch");
        ViewBag.OnHoldCount = allItems.Count(aw => aw.Status == "On-Hold");
        ViewBag.DroppedCount = allItems.Count(aw => aw.Status == "Dropped");

        var resultList = await query.OrderByDescending(aw => aw.AnimeWatchListId).ToListAsync();
        return View(resultList);
    }

    // POST: /Watchlist/SaveOrUpdate
    [HttpPost]
    public async Task<IActionResult> SaveOrUpdate(int animeId, string status, int progress)
    {
        int userId = GetCurrentUserId();

        // 1. Locate member
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member == null) return RedirectToAction("Index", "Anime");

        // 2. Locate or create member's primary WatchList record
        var watchList = await db.WatchLists.FirstOrDefaultAsync(w => w.UserId == userId);

        if (watchList == null)
        {
            watchList = new WatchList
            {
                UserId = userId
            };
            db.WatchLists.Add(watchList);
            await db.SaveChangesAsync(); // Persist parent record to generate WatchListId
        }

        // 3. Locate target anime
        var anime = await db.Animes.FindAsync(animeId);
        if (anime == null) return RedirectToAction("Index", "Anime");

        // 4. Update existing entry or append new entry
        var entry = await db.AnimeWatchLists
            .FirstOrDefaultAsync(aw => aw.WatchListId == watchList.WatchListId && aw.AnimeId == animeId);

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (entry == null)
        {
            db.AnimeWatchLists.Add(new AnimeWatchList
            {
                WatchListId = watchList.WatchListId,
                AnimeId = animeId,
                Status = status,
                Progress = progress,
                StartDate = status == "Watching" ? today : null,
                EndDate = status == "Completed" ? today : null
            });
        }
        else
        {
            entry.Status = status;
            entry.Progress = progress;
            if (status == "Completed") entry.EndDate = today;
        }

        await db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    // GET: /Watchlist/QuickAdd?animeId=1
    public async Task<IActionResult> QuickAdd(int animeId)
    {
        var anime = await db.Animes.FirstOrDefaultAsync(a => a.AnimeId == animeId);
        if (anime == null)
        {
            return RedirectToAction("Index", "Anime");
        }

        return View(anime);
    }

    // GET: /Watchlist/Stats
    public async Task<IActionResult> Stats()
    {
        int userId = GetCurrentUserId();

        var watchListItems = await db.AnimeWatchLists
            .Include(aw => aw.Anime)
            .Where(aw => aw.WatchList.UserId == userId)
            .ToListAsync();

        var stats = new ProgressStatsVM
        {
            TotalAnimesInList = watchListItems.Count,
            TotalEpisodesWatched = watchListItems.Sum(aw => aw.Progress),
            WatchingCount = watchListItems.Count(aw => aw.Status == "Watching"),
            CompletedCount = watchListItems.Count(aw => aw.Status == "Completed"),
            PlanToWatchCount = watchListItems.Count(aw => aw.Status == "Plan to Watch"),
            OnHoldCount = watchListItems.Count(aw => aw.Status == "On-Hold"),
            DroppedCount = watchListItems.Count(aw => aw.Status == "Dropped"),
            CompletionRatePercentage = watchListItems.Count > 0
                ? Math.Round((double)watchListItems.Count(aw => aw.Status == "Completed") / watchListItems.Count * 100, 2)
                : 0,
            RecentlyCompleted = watchListItems
                .Where(aw => aw.Status == "Completed" && aw.EndDate.HasValue)
                .OrderByDescending(aw => aw.EndDate)
                .Take(5)
                .ToList()
        };

        return View(stats);
    }
}
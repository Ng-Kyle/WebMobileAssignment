using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebMobileAssignment.Models;

namespace WebMobileAssignment.Controllers
{
    public class WatchlistController(DB db) : Controller
    {
        public int GetCurrentUserId()
        {
            return 1;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            int memberId = GetCurrentUserId();

            var query = db.AnimeWatchLists
                .Include(aw => aw.Anime).ThenInclude(a => a.AnimeImages)
                .Include(aw => aw.Anime).ThenInclude(a => a.Studio)
                .Where(aw => aw.WatchList.Member.UserId == memberId)
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

            var allItems = db.AnimeWatchLists
                .Where(aw => aw.WatchList.Member.UserId == memberId)
                .ToList();

            ViewBag.TotalCount = allItems.Count;
            ViewBag.WatchingCount = allItems.Count(aw => aw.Status == "Watching");
            ViewBag.CompletedCount = allItems.Count(aw => aw.Status == "Completed");
            ViewBag.PlanCount = allItems.Count(aw => aw.Status == "Plan to Watch");
            ViewBag.OnHoldCount = allItems.Count(aw => aw.Status == "On-Hold");
            ViewBag.DroppedCount = allItems.Count(aw => aw.Status == "Dropped");

            return View(query.OrderByDescending(aw => aw.AnimeWatchListId).ToList());
        }

        [HttpPost]
        public IActionResult SaveOrUpdate(int animeId, string status, int progress)
        {
            int memberId = GetCurrentUserId();
            if (memberId == 0) return;

            var watchListId = db.WatchLists
                .Where(w => w.Member.UserId == memberId)
                .Select(w => w.WatchListId)
                .FirstOrDefault();

            var anime = db.Animes.Find(animeId);
            if (anime == null) return RedirectToAction("Index", "Anime");

            var entry = db.AnimeWatchLists
                .FirstOrDefault(aw => aw.WatchListId == watchListId && aw.AnimeId == animeId);

        var today = DateOnly.FromDateTime(DateTime.Today);

            if (entry == null)
            {
                db.AnimeWatchLists.Add(new AnimeWatchList
                {
                    WatchListId = watchListId,
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
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
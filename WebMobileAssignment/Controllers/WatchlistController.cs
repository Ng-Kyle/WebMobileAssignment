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
    [Authorize(Roles = "Member")]
    public class WatchlistController(DB db) : Controller
    {
        public int GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(idClaim, out int id))
            {
                return id;
            }

            var identifier = User.Identity?.Name;
            if (!string.IsNullOrEmpty(identifier))
            {
                var member = db.Members.FirstOrDefault(m => m.Email == identifier || m.UserName == identifier);
                if (member != null)
                {
                    return member.UserId;
                }
            }

            return 0;
        }
        

        public IActionResult Index(string? search, string? status)
        {
            int memberId = GetCurrentUserId();

            var baseQuery = db.AnimeWatchLists
                .Include(aw => aw.Anime).ThenInclude(a => a.AnimeImages)
                .Include(aw => aw.Anime).ThenInclude(a => a.Studio)
                .Where(aw => aw.WatchList.Member.UserId == memberId);

            var allItems = baseQuery.ToList();
            ViewBag.TotalCount = allItems.Count;
            ViewBag.WatchingCount = allItems.Count(aw => aw.Status == "Watching");
            ViewBag.CompletedCount = allItems.Count(aw => aw.Status == "Completed");
            ViewBag.PlanCount = allItems.Count(aw => aw.Status == "Plan to Watch");
            ViewBag.OnHoldCount = allItems.Count(aw => aw.Status == "On-Hold");
            ViewBag.DroppedCount = allItems.Count(aw => aw.Status == "Dropped");

            var query = allItems.AsQueryable();

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

            var model = query.OrderByDescending(aw => aw.AnimeWatchListId).ToList();

            if (Request.IsAjax())
            {
                return PartialView("_WatchlistTable", model);
            }

            return View("Index", model);

        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult QuickAdd(int animeId)
        {
            SaveOrUpdateRecord(animeId, "Plan to Watch", 0);
            return Ok(new { success = true });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveOrUpdate(int animeId, string status, int progress, string? currentFilterStatus, string? search)
        {
            SaveOrUpdateRecord(animeId, status, progress);
            return Index(search, currentFilterStatus);
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        public void SaveOrUpdateRecord(int animeId, string status, int progress)
        {
            int memberId = GetCurrentUserId();
            if (memberId == 0) return;

            var member = db.Members.FirstOrDefault(m => m.UserId == memberId);
            if (member == null) return;

            var watchList = db.WatchLists.FirstOrDefault(w => w.Member.UserId == memberId);
            if (watchList == null)
            {
                watchList = new WatchList { Member = member, UserId = member.UserId };
                db.WatchLists.Add(watchList);
                db.SaveChanges();
            }

            var anime = db.Animes.Find(animeId);
            if (anime == null) return;

            var entry = db.AnimeWatchLists
                .FirstOrDefault(aw => aw.WatchListId == watchList.WatchListId && aw.AnimeId == animeId);

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (status == "Completed")
            {
                progress = anime.Episodes;
            }
            else if (status == "Plan to Watch")
            {
                progress = 0;
            }

            int clampedProgress = Math.Clamp(progress, 0, anime.Episodes);

            if (entry == null)
            {
                db.AnimeWatchLists.Add(new AnimeWatchList
                {
                    WatchListId = watchList.WatchListId,
                    AnimeId = animeId,
                    Status = status,
                    Progress = Math.Clamp(progress, 0, anime.Episodes),
                    StartDate = status == "Watching" ? today : null,
                    EndDate = status == "Completed" ? today : null
                });
            }
            else
            {
                entry.Status = status;
                entry.Progress = Math.Clamp(progress, 0, anime.Episodes);
                if (status == "Completed" && entry.EndDate == null) entry.EndDate = today;
                if (status == "Watching" && entry.StartDate == null) entry.StartDate = today;
            }
            db.SaveChanges();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult UpdateProgress(int animeWatchListId, int step)
        {
            int memberId = GetCurrentUserId();

            var entry = db.AnimeWatchLists
                .Include(aw => aw.Anime)
                .FirstOrDefault(aw => aw.AnimeWatchListId == animeWatchListId && aw.WatchList.Member.UserId == memberId);

            if ( entry != null)
            {
                entry.Progress = Math.Clamp(entry.Progress + step, 0, entry.Anime.Episodes);

                if (entry.Progress == entry.Anime.Episodes)
                {
                    entry.Status = "Completed";
                    entry.EndDate = DateOnly.FromDateTime(DateTime.Today);
                }
                else if (entry.Progress > 0 && entry.Status == "Plan to Watch")
                {
                    entry.Status = "Watching";
                    entry.StartDate = DateOnly.FromDateTime(DateTime.Today);
                }

                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Remove(int animeWatchListId)
        {
            int memberId = GetCurrentUserId();

            var entry = db.AnimeWatchLists
                .FirstOrDefault(aw => animeWatchListId == animeWatchListId && aw.WatchList.Member.UserId == memberId);

            if (entry != null)
            {
                db.AnimeWatchLists.Remove(entry);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SetProgress(int animeWatchListId, int newProgress, string? currentFilterStatus, string? search)
        {
            int memberId = GetCurrentUserId();

            var entry = db.AnimeWatchLists
                .Include(aw => aw.Anime)
                .FirstOrDefault(aw => aw.AnimeWatchListId == animeWatchListId && aw.WatchList.Member.UserId == memberId);

            if (entry != null)
            {
                entry.Progress = Math.Clamp(newProgress, 0, entry.Anime.Episodes);

                var today = DateOnly.FromDateTime(DateTime.Today);

                if (entry.Progress == entry.Anime.Episodes)
                {
                    entry.Status = "Completed";
                    entry.EndDate = today;
                }
                else if (entry.Progress > 0)
                {
                    if (entry.Status == "Plan to Watch")
                    {
                        entry.Status = "Watching";
                    }
                    if (entry.StartDate == null)
                    {
                        entry.StartDate = today;
                    }
                }

                db.SaveChanges();
            }

            return Index(search, currentFilterStatus);
        }

        [HttpGet]
        public IActionResult ExportCsv()
        {
            int memberId = GetCurrentUserId();
            if (memberId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var list = db.AnimeWatchLists
                .Include(aw => aw.Anime)
                .Where(aw => aw.WatchList.Member.UserId == memberId)
                .OrderBy(aw => aw.Status)
                .ThenBy(aw => aw.Anime.Name)
                .ToList();

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Anime Title,Status,Episodes Watched,Total Episodes,Start Date,End Date");

            foreach (var item in list)
            {
                string title = $"\"{item.Anime.Name.Replace("\"", "\"\"")}\"";
                string startDate = item.StartDate?.ToString("yyyy-MM-dd") ?? "N/A";
                string endDate = item.EndDate?.ToString("yyyy-MM-dd") ?? "N/A";

                builder.AppendLine($"{title},{item.Status},{item.Progress},{item.Anime.Episodes},{startDate},{endDate}");
            }

            byte[] preamble = System.Text.Encoding.UTF8.GetPreamble();
            byte[] content = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
            byte[] fileBytes = [.. preamble, .. content];

            string fileName = $"AnonAnimeList_Watchlist_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(fileBytes, "text/csv", fileName);
        }
    }
}

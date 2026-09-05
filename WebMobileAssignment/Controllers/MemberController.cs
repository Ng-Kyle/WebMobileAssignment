using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace WebMobileAssignment.Controllers;

public class MemberController (DB db, Helper hp): Controller
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
            TempData["Info"] = "No member found!";
            return RedirectToAction("Management");
        }
        user.IsBlocked = true;
        db.SaveChanges();
        TempData["Info"] = user.UserName + " has been blocked.";
        return RedirectToAction("Management");
    }
    [HttpPost]
    public IActionResult Unblock(int userId)
    {
        var user = db.Members.Find(userId);
        if (user == null)
        {
            TempData["Info"] = "No member found!";
            return RedirectToAction("Management");
        }
        user.IsBlocked = false;
        db.SaveChanges();
        TempData["Info"] = user.UserName + " has been unblocked.";
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
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace WebMobileAssignment.Controllers;

public class GenreController (DB db, Helper hp): Controller
{
    public IActionResult Management(string? name, string? sort, string? direction, int page = 1)
    {
        ViewBag.Name = name = name?.Trim() ?? "";
        var searched = db.Genres.Where(a => a.Name.Contains(name));

        ViewBag.Sort = sort;
        ViewBag.Direction = direction;

        Func<Genre, object> fSort = sort switch
        {
            "Name" => genre => genre.Name,
            _ => genre => genre.Name
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
            return PartialView("AdminGenreTable", a);
        }
        var vm = new GenreManagementVM
        {
            Genres = a,
        };
        return View(vm);
    }
    [HttpPost]
    public IActionResult Add(GenreManagementVM GenreManagementmentVM )
    {
        if (db.Genres.Any(genre => genre.Name.Equals(GenreManagementmentVM.Name)))
        {
            ModelState.AddModelError("Name", "This studio already exists.");
        }
        
        if (ModelState.IsValid)
        {
            db.Genres.Add(new()
            {
                Name = GenreManagementmentVM.Name,
            });
            db.SaveChanges();
            TempData["Info"] = "Genre has been added.";
            return RedirectToAction("Management");
        }

        GenreManagementmentVM.Genres = db.Genres.Include(a => a.Animes).ToList().ToPagedList(1, 10);
        return View("Management", GenreManagementmentVM);
    }
    [HttpPost]
    public IActionResult Delete(int genreId)
    {
        var genre = db.Genres.Include(a => a.Animes).FirstOrDefault(g => g.GenreId == genreId);
        if (genre == null)
        {
            TempData["Info"] = "No genre found!";
            return RedirectToAction("Management");
        }
        if (genre.Animes.Count > 0)
        {
            TempData["Info"] = "Can't delete a genre that has released animes in the database!";
            return RedirectToAction("Management");
        }
        db.Genres.Remove(genre);
        db.SaveChanges();

        TempData["Info"] = "Genre has been deleted.";
        return RedirectToAction("Management");
    }
}

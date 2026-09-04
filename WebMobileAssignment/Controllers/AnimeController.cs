using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace WebMobileAssignment.Controllers;

public class AnimeController(DB db, Helper hp) : Controller
{
    public IActionResult Index()
    {
        return View();
    }
    //ADMIN SECTION
    public IActionResult Management(string? name, int? StudioId, int? GenreId, string? Status, string? sort, string? direction, int page = 1)
    {
        ViewBag.Name = name = name?.Trim() ?? "";
        var searched = db.Animes.Include(a => a.Studio).Include(a => a.AnimeImages).Include(a => a.Genres).Where(a => a.Name.Contains(name));

        string[] status =["Upcoming","Airing","Completed"]; 
        ViewBag.Sort = sort;
        ViewBag.Direction = direction;
        ViewBag.StatusList = new SelectList(status);
        ViewBag.StudioList = new SelectList(db.Studios, "StudioId", "Name");
        ViewBag.GenreList = new SelectList(db.Genres, "GenreId", "Name");
        ViewBag.GenreId = GenreId;
        ViewBag.Status = Status;
        ViewBag.StudioId = StudioId;

        if(StudioId  != null)
        {
            searched = searched.Where(a => a.Studio.StudioId == StudioId);
        }
        if (GenreId != null)
        {
            searched = searched.Where(a => a.Genres.Any(g => g.GenreId == GenreId));
        }
        if (Status != null)
        {
            searched = searched.Where(a => a.Status == Status);
        }
        Func<Anime, object> fSort = sort switch
        {
            "Name" => anime => anime.Name,
            "Episodes" => anime => anime.Episodes,
            "Release Date" => anime => anime?.ReleaseDate,
            _ => anime => anime.Name
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
            return PartialView("AdminAnimeTable", a);
        }
        var vm = new AnimeManagementVM
        {
            Animes = a
        };
        AnimeVM animeVM = new()
        {
            Genres = db.Genres.Select(g => new GenreVM { GenreID = g.GenreId, Name = g.Name }).ToList()
        };

        vm.AnimeVMs.Add(animeVM);
        return View(vm);
    }
    [HttpPost]
    public IActionResult Add(AnimeManagementVM AdminManagementVM)
    {
        AnimeVM vm = AdminManagementVM.AnimeVMs[0];
        string[] status = ["Upcoming", "Airing", "Completed"];

        if(ModelState.IsValid("AnimeVMs[0].StudioId") && !db.Studios.Any(s => s.StudioId == vm.StudioId))
        {
            ModelState.AddModelError("AnimeVMs[0].StudioId", "Please select a valid studio.");
        }
        if (db.Animes.Any(anime => anime.Name.Equals(vm.Name)))
        {
            ModelState.AddModelError("AnimeVMs[0].Name", "This anime already exists.");
        }
        if (!status.Contains(vm.Status))
        {
            ModelState.AddModelError("AnimeVMs[0].Status", "Please select a valid status.");
        }
        if(!vm.Genres.Any(g => g.Selected))
        {
            ModelState.AddModelError("AnimeVMs[0].Genres", "Please select at least one genre");
        }
        foreach(var genre in vm.Genres.Where(g => g.Selected))
        {
            if(!db.Genres.Any(g => g.GenreId == genre.GenreID))
            {
                ModelState.AddModelError("AnimeVMs[0].Genres","One or more selected genres are invalid.");
                break;
            }
        }
        if (vm.AnimeImageFiles != null && ModelState.IsValid("AnimeVMs[0].AnimeImageFiles"))
        {
            foreach (var photo in vm.AnimeImageFiles)
            {
                var e = hp.ValidatePhoto(photo);
                if (e != "")
                {
                    ModelState.AddModelError("AnimeVMs[0].AnimeImageFiles", e);
                    break;
                }
            }
        }
        if (vm.Status != status[0] && ModelState.IsValid("AnimeVMs[0].ReleaseDate"))
        {
            if(vm.ReleaseDate == null || vm.ReleaseDate > DateTime.Today.ToDateOnly())
            {
                ModelState.AddModelError("AnimeVMs[0].ReleaseDate", "Airing and Completed anime must have a date today or in the past!");
            }
        }
        if (ModelState.IsValid)
        {
            Anime anime = new()
            {
                Name = vm.Name,
                Episodes = vm.Episodes,
                Status = vm.Status,
                Synopsis = vm.Synopsis,
                TrailerLink = vm.TrailerLink,
                ReleaseDate = vm.ReleaseDate,
                Studio = db.Studios.First(s => s.StudioId == vm.StudioId),

            };
            foreach (var genre in vm.Genres.Where(g => g.Selected))
            {
                anime.Genres.Add(db.Genres.First(g => g.GenreId == genre.GenreID));
            }
            if (vm.AnimeImageFiles != null)
            {
                int displayOrder = 1;
                foreach (var photo in vm.AnimeImageFiles)
                {
                    var imageURL = hp.SavePhoto(photo, "anime");
                    anime.AnimeImages.Add(new()
                    {
                        ImageLink = imageURL,
                        IsThumbnail = displayOrder == 1,
                        DisplayOrder = displayOrder++,
                    });
                }
            }
            db.Animes.Add(anime);
            db.SaveChanges();
            TempData["Info"] = "Anime has been added.";
            return RedirectToAction("Management");
        }
        ViewBag.StatusList = new SelectList(status);
        ViewBag.StudioList = new SelectList(db.Studios, "StudioId", "Name");
        ViewBag.GenreList = new SelectList(db.Genres, "GenreId", "Name");
        AdminManagementVM.Animes = db.Animes.Include(a => a.Studio).Include(a => a.AnimeImages).Include(a => a.Genres).ToList().ToPagedList(1, 10);
        return View("Management", AdminManagementVM);
    }

    public IActionResult ManagementDetails(int animeId)
    {
        var anime = db.Animes.Include(a => a.Studio).Include(a => a.AnimeImages).Include(a => a.Genres).FirstOrDefault(a => a.AnimeId == animeId);
        if (anime == null)
        {
            return RedirectToAction("Management");
        }
        AnimeVM animeVM = new()
        {
            AnimeID = animeId,
            Name = anime.Name,
            Episodes = anime.Episodes,
            Synopsis = anime.Synopsis,
            ReleaseDate = anime.ReleaseDate,
            TrailerLink = anime.TrailerLink,
            Status = anime.Status,
            StudioId = anime.Studio.StudioId,
            AnimeImages = anime.AnimeImages,
        };
        animeVM.Genres = db.Genres.ToList().Select(g => new GenreVM
        {
            GenreID = g.GenreId,
            Name = g.Name,
            Selected = anime.Genres.Any(ag => ag.GenreId == g.GenreId)
        }).ToList();
        string[] status = ["Upcoming", "Airing", "Completed"];
        ViewBag.StatusList = new SelectList(status);
        ViewBag.StudioList = new SelectList(db.Studios, "StudioId", "Name");
        return View(animeVM);
    }
    [HttpPost]
    public IActionResult Edit(AnimeVM animeVM)
    {
        var anime = db.Animes.Include(a => a.Studio).Include(a => a.AnimeImages).Include(a => a.Genres).FirstOrDefault(a => a.AnimeId == animeVM.AnimeID);
        if (anime == null)
        {
            return RedirectToAction("Management");
        }
        string[] status = ["Upcoming", "Airing", "Completed"];
        if (ModelState.IsValid("StudioId") && !db.Studios.Any(s => s.StudioId == animeVM.StudioId))
        {
            ModelState.AddModelError("StudioId", "Please select a valid studio.");
        }
        if (db.Animes.Any(a => a.Name.Equals(animeVM.Name) && a.AnimeId != animeVM.AnimeID))
        {
            ModelState.AddModelError("Name", "This anime already exists.");
        }
        if (!status.Contains(animeVM.Status))
        {
            ModelState.AddModelError("Status", "Please select a valid status.");
        }
        if (!animeVM.Genres.Any(g => g.Selected))
        {
            ModelState.AddModelError("Genres", "Please select at least one genre");
        }
        foreach (var genre in animeVM.Genres.Where(g => g.Selected))
        {
            if (!db.Genres.Any(g => g.GenreId == genre.GenreID))
            {
                ModelState.AddModelError("Genres", "One or more selected genres are invalid.");
                break;
            }
        }
        if (animeVM.AnimeImageFiles != null && ModelState.IsValid("AnimeImageFiles"))
        {
            foreach (var photo in animeVM.AnimeImageFiles)
            {
                var e = hp.ValidatePhoto(photo);
                if (e != "")
                {
                    ModelState.AddModelError("AnimeImageFiles", e);
                    break;
                }
            }
        }
        if (ModelState.IsValid)
        {
            anime.Name = animeVM.Name;
            anime.Episodes = animeVM.Episodes;
            anime.Status = animeVM.Status;
            anime.Synopsis = animeVM.Synopsis;
            anime.TrailerLink = animeVM.TrailerLink;
            anime.ReleaseDate = animeVM.ReleaseDate;
            anime.Studio = db.Studios.First(s => s.StudioId == animeVM.StudioId);
            anime.Genres.Clear();
            foreach (var genre in animeVM.Genres.Where(g => g.Selected))
            {
                anime.Genres.Add(db.Genres.First(g => g.GenreId == genre.GenreID));
            }
            if (animeVM.AnimeImageFiles?.Count > 0)
            {
                int displayOrder = 1;
                foreach (var image in anime.AnimeImages)
                {
                    hp.DeletePhoto(image.ImageLink, "anime");
                }
                anime.AnimeImages.Clear();
                foreach (var photo in animeVM.AnimeImageFiles)
                {
                    var imageURL = hp.SavePhoto(photo, "anime");
                    anime.AnimeImages.Add(new()
                    {
                        ImageLink = imageURL,
                        IsThumbnail = displayOrder == 1,
                        DisplayOrder = displayOrder++,
                    });
                }
            }

            db.SaveChanges();
            TempData["Info"] = "Anime has been updated.";
            return RedirectToAction("Management");
        }
        animeVM.Genres = db.Genres.ToList().Select(g => new GenreVM
        {
            GenreID = g.GenreId,
            Name = g.Name,
            Selected = anime.Genres.Any(ag => ag.GenreId == g.GenreId)
        }).ToList();
        ViewBag.StatusList = new SelectList(status);
        ViewBag.StudioList = new SelectList(db.Studios, "StudioId", "Name");
        animeVM.AnimeImages = anime.AnimeImages;
        return View("ManagementDetails",animeVM);
    }
    [HttpPost]
    public IActionResult Delete(int animeId)
    {
        var anime = db.Animes.Include(a => a.AnimeImages).FirstOrDefault(a => a.AnimeId == animeId);
        if (anime != null)
        {
            foreach (var image in anime.AnimeImages)
            {
                hp.DeletePhoto(image.ImageLink, "anime");
            }
            db.Animes.Remove(anime);
            db.SaveChanges();
            TempData["Info"] = "Anime has been deleted.";
        }
        return RedirectToAction("Management");
    }
}

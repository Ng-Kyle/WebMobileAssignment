using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using WebMobileAssignment.Models;

namespace WebMobileAssignment.Controllers;

public class AnimeController(DB db, Helper hp, IStringLocalizer<SharedResource> localizer) : Controller
{
    // ANIME CATALOGUE
    public IActionResult Index(string? search, int? studioId, List<int> genreIds, int? min, int? max)
    {
        search = search?.Trim() ?? "";

        var query = db.Animes
            .Include(a => a.AnimeImages)
            .Include(a => a.Reviews)
            .Include(a => a.Genres)
            .Include(a => a.Studio)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.Name.Contains(search));
            ViewBag.Search = search;
        }

        if (studioId.HasValue)
        {
            query = query.Where(a => a.Studio.StudioId == studioId.Value);
            ViewBag.SelectedStudioId = studioId.Value;
        }

        if (genreIds != null && genreIds.Any())
        {
            query = query.Where(a => a.Genres.Any(g => genreIds.Contains(g.GenreId)));
            ViewBag.SelectedGenreIds = genreIds;
        }

        if (min.HasValue)
        {
            query = query.Where(a => a.Episodes >= min.Value);
            ViewBag.Min = min.Value;
        }

        if (max.HasValue)
        {
            query = query.Where(a => a.Episodes <= max.Value);
            ViewBag.Max = max.Value;
        }

        ViewBag.Studios = db.Studios.ToList();
        ViewBag.Genres = db.Genres.ToList();

        var m = query.ToList();

        if (Request.IsAjax())
        {
            return PartialView("_AnimeGrid", m);
        }

        return View(m);
    }

    // GET: /Anime/Details/5
    public async Task<IActionResult> Details(int? id, int? ratingFilter, string sort = "newest")
    {
        if (!id.HasValue)
        {
            return RedirectToAction(nameof(Index));
        }

        var anime = await db.Animes
            .Include(a => a.AnimeImages)
            .Include(a => a.Genres)
            .Include(a => a.Studio)
            .Include(a => a.Reviews)
                .ThenInclude(r => r.Member)
            .FirstOrDefaultAsync(a => a.AnimeId == id.Value);

        if (anime == null)
        {
            return NotFound();
        }

        // Apply rating filter and sorting on reviews
        if (anime.Reviews != null && anime.Reviews.Any())
        {
            var reviewsQuery = anime.Reviews.AsEnumerable();

            if (ratingFilter.HasValue && ratingFilter > 0)
            {
                reviewsQuery = reviewsQuery.Where(r => r.Rating == ratingFilter.Value);
            }

            anime.Reviews = sort switch
            {
                "oldest" => reviewsQuery.OrderBy(r => r.CreatedAt).ToList(),
                "highest" => reviewsQuery.OrderByDescending(r => r.Rating).ToList(),
                "lowest" => reviewsQuery.OrderBy(r => r.Rating).ToList(),
                _ => reviewsQuery.OrderByDescending(r => r.CreatedAt).ToList()
            };
        }

        // Identify logged-in member & existing review
        int? currentUserId = HttpContext.Session.GetInt32("UserId");
        ViewBag.CurrentUserId = currentUserId;

        if (currentUserId.HasValue && anime.Reviews != null)
        {
            ViewBag.MyReview = anime.Reviews.FirstOrDefault(r => r.Member?.UserId == currentUserId.Value);
        }

        ViewBag.ReviewSort = sort;
        ViewBag.CurrentRatingFilter = ratingFilter;

        return View(anime);
    }

    // ADMIN SECTION
    public IActionResult Management(string? name, int? StudioId, int? GenreId, string? Status, string? sort, string? direction, int page = 1)
    {
        ViewBag.Name = name = name?.Trim() ?? "";
        var searched = db.Animes.Include(a => a.Studio).Include(a => a.AnimeImages).Include(a => a.Genres).Where(a => a.Name.Contains(name));

        string[] status = ["Upcoming", "Airing", "Completed"];
        ViewBag.Sort = sort;
        ViewBag.Direction = direction;
        ViewBag.StatusList = new SelectList(status);
        ViewBag.StudioList = new SelectList(db.Studios, "StudioId", "Name");
        ViewBag.GenreList = new SelectList(db.Genres, "GenreId", "Name");
        ViewBag.GenreId = GenreId;
        ViewBag.Status = Status;
        ViewBag.StudioId = StudioId;

        if (StudioId != null)
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

        var a = sorted.ToPagedList(page, 5);
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
        string[] status = ["Upcoming", "Airing", "Completed"];
        for (int i = 0; i < AdminManagementVM.AnimeVMs.Count; i++)
        {
            AnimeVM vm = AdminManagementVM.AnimeVMs[i];
            if (ModelState.IsValid($"AnimeVMs[{i}].StudioId") && !db.Studios.Any(s => s.StudioId == vm.StudioId))
            {
                ModelState.AddModelError($"AnimeVMs[{i}].StudioId", localizer["Please select a valid studio."]);
            }
            if (db.Animes.Any(anime => anime.Name.Equals(vm.Name)))
            {
                ModelState.AddModelError($"AnimeVMs[{i}].Name", localizer["This anime already exists."]);
            }
            for (int j = 0; j < i; j++)
            {
                AnimeVM pastVM = AdminManagementVM.AnimeVMs[j];
                if (string.Equals(pastVM.Name, vm.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError($"AnimeVMs[{i}].Name", localizer["This anime already exists."]);
                    break;
                }
            }
            if (!status.Contains(vm.Status))
            {
                ModelState.AddModelError($"AnimeVMs[{i}].Status", localizer["Please select a valid status."]);
            }
            if (!vm.Genres.Any(g => g.Selected))
            {
                ModelState.AddModelError($"AnimeVMs[{i}].Genres", localizer["Please select at least one genre."]);
            }
            foreach (var genre in vm.Genres.Where(g => g.Selected))
            {
                if (!db.Genres.Any(g => g.GenreId == genre.GenreID))
                {
                    ModelState.AddModelError($"AnimeVMs[{i}].Genres", localizer["One or more selected genres are invalid."]);
                    break;
                }
            }
            if (vm.AnimeImageFiles != null && ModelState.IsValid($"AnimeVMs[{i}].AnimeImageFiles"))
            {
                foreach (var photo in vm.AnimeImageFiles)
                {
                    var e = hp.ValidatePhoto(photo);
                    if (e != "")
                    {
                        ModelState.AddModelError($"AnimeVMs[{i}].AnimeImageFiles", e);
                        break;
                    }
                }
            }
            if (vm.Status != status[0] && ModelState.IsValid($"AnimeVMs[{i}].ReleaseDate"))
            {
                if (vm.ReleaseDate == null || vm.ReleaseDate > DateTime.Today.ToDateOnly())
                {
                    ModelState.AddModelError($"AnimeVMs[{i}].ReleaseDate", localizer["Airing and Completed anime must have a date today or in the past!"]);
                }
            }
        }

        if (ModelState.IsValid)
        {
            foreach (var vm in AdminManagementVM.AnimeVMs)
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
                        var imageURL = hp.SavePhoto(photo, "images/anime");
                        anime.AnimeImages.Add(new()
                        {
                            ImageLink = imageURL,
                            IsThumbnail = displayOrder == 1,
                            DisplayOrder = displayOrder++,
                        });
                    }
                }
                db.Animes.Add(anime);
            }
            db.SaveChanges();
            TempData["Info"] = localizer["AnimesAddedCount", AdminManagementVM.AnimeVMs.Count].Value;
            return RedirectToAction("Management");
        }
        ViewBag.StatusList = new SelectList(status);
        ViewBag.StudioList = new SelectList(db.Studios, "StudioId", "Name");
        ViewBag.GenreList = new SelectList(db.Genres, "GenreId", "Name");
        AdminManagementVM.Animes = db.Animes.Include(a => a.Studio).Include(a => a.AnimeImages).Include(a => a.Genres).ToList().ToPagedList(1, 5);
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
            ModelState.AddModelError("StudioId", localizer["Please select a valid studio."]);
        }
        if (db.Animes.Any(a => a.Name.Equals(animeVM.Name) && a.AnimeId != animeVM.AnimeID))
        {
            ModelState.AddModelError("Name", localizer["This anime already exists."]);
        }
        if (!status.Contains(animeVM.Status))
        {
            ModelState.AddModelError("Status", localizer["Please select a valid status."]);
        }
        if (!animeVM.Genres.Any(g => g.Selected))
        {
            ModelState.AddModelError("Genres", localizer["Please select at least one genre."]);
        }
        foreach (var genre in animeVM.Genres.Where(g => g.Selected))
        {
            if (!db.Genres.Any(g => g.GenreId == genre.GenreID))
            {
                ModelState.AddModelError("Genres", localizer["One or more selected genres are invalid."]);
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
                    hp.DeletePhoto(image.ImageLink, "images/anime");
                }
                anime.AnimeImages.Clear();
                foreach (var photo in animeVM.AnimeImageFiles)
                {
                    var imageURL = hp.SavePhoto(photo, "images/anime");
                    anime.AnimeImages.Add(new()
                    {
                        ImageLink = imageURL,
                        IsThumbnail = displayOrder == 1,
                        DisplayOrder = displayOrder++,
                    });
                }
            }

            db.SaveChanges();
            TempData["Info"] = localizer["Anime has been updated."].Value;
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
        return View("ManagementDetails", animeVM);
    }

    [HttpPost]
    public IActionResult Delete(int animeId)
    {
        var anime = db.Animes.Include(a => a.AnimeImages).FirstOrDefault(a => a.AnimeId == animeId);
        if (anime != null)
        {
            foreach (var image in anime.AnimeImages)
            {
                hp.DeletePhoto(image.ImageLink, "images/anime");
            }
            db.Animes.Remove(anime);
            db.SaveChanges();
            TempData["Info"] = localizer["Anime has been deleted."].Value;
        }
        return RedirectToAction("Management");
    }

    [HttpPost]
    public IActionResult DeleteMany(int[] animeIds)
    {
        if (animeIds.Length == 0)
        {
            TempData["Info"] = localizer["No animes were selected."].Value;
            return RedirectToAction("Management");
        }
        int n = db.Animes.Where(s => animeIds.Contains(s.AnimeId)).ExecuteDelete();

        TempData["Info"] = localizer["AnimeDeletedCount", n].Value;
        return RedirectToAction("Management");
    }
}
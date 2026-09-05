using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
namespace WebMobileAssignment.Controllers;

public class StudioController(DB db, Helper hp) : Controller
{
    public IActionResult Management(string? name, string? sort, string? direction, int page = 1)
    {
        ViewBag.Name = name = name?.Trim() ?? "";
        var searched = db.Studios.Include(a => a.Animes).Where(a => a.Name.Contains(name));

        ViewBag.Sort = sort;
        ViewBag.Direction = direction;
        
        Func<Studio, object> fSort = sort switch
        {
            "Name" => studio => studio.Name,
            "Animes Released" => studio => studio.Animes.Count,
            "Date Established" => studio => studio.DateEstablished,
            _ => studio => studio.Name
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
            return PartialView("AdminStudioTable", a);
        }
        var vm = new StudioManagementVM
        {
            Studios = a,
            StudioVM = new StudioVM() { DateEstablished = DateTime.Today.ToDateOnly()}
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult Add(StudioManagementVM StudioManagementVM)
    {
        StudioVM vm = StudioManagementVM.StudioVM;

       
        if (db.Studios.Any(studio => studio.Name.Equals(vm.Name)))
        {
            ModelState.AddModelError("StudioVM.Name", "This studio already exists.");
        }
        if(vm.DateEstablished > DateTime.Today.ToDateOnly())
        {
            ModelState.AddModelError("StudioVM.DateEstablished", "The date cannot be in the future.");
        }
        if (vm.StudioImage == null)
        {
            ModelState.AddModelError("StudioVM.StudioImage", "An image of the studio's logo is required.");
        }
        else
        {
            var e = hp.ValidatePhoto(vm.StudioImage);
            if (e != "")
            {
                ModelState.AddModelError("StudioVM.StudioImage", e);
            }
        }
        if (ModelState.IsValid)
        {

            db.Studios.Add(new() {
                Name = vm.Name,
                Description = vm.Description,
                DateEstablished = vm.DateEstablished,
                ImageLink = hp.SavePhoto(vm.StudioImage, "images/studio")
            });
            db.SaveChanges();
            TempData["Info"] = "Studio has been added.";
            return RedirectToAction("Management");
        }
       
        StudioManagementVM.Studios = db.Studios.Include(a => a.Animes).ToList().ToPagedList(1, 5);
        return View("Management", StudioManagementVM);
    }
    [HttpPost]
    public IActionResult Delete(int studioId)
    {
        var studio = db.Studios.Include(a => a.Animes).FirstOrDefault(s => s.StudioId == studioId);
        if(studio == null)
        {
            TempData["Info"] = "No studio found!";
            return RedirectToAction("Management");
        }
        if(studio.Animes.Count > 0)
        {
            TempData["Info"] = "Can't delete a studio that has released animes in the database!";
            return RedirectToAction("Management");
        }
        
        hp.DeletePhoto(studio.ImageLink, "images/studio");
        db.Studios.Remove(studio);
        db.SaveChanges();

        TempData["Info"] = "Studio has been deleted.";

        return RedirectToAction("Management");
    }
    public IActionResult ManagementDetails(int studioId)
    {
        var studio  = db.Studios.Include(a => a.Animes).ThenInclude(a => a.AnimeImages).FirstOrDefault(s => s.StudioId == studioId);
        if (studio == null)
        {
            return RedirectToAction("Management");
        }
        StudioVM studioVM = new()
        {
            StudioID = studioId,
            Name = studio.Name,
            Description = studio.Description,
            DateEstablished = studio.DateEstablished,
            ImageLink = studio.ImageLink,
            Animes = studio.Animes,
        };
        return View(studioVM);
    }
    [HttpPost]
    public IActionResult Edit(StudioVM studioVM)
    {
        var studio = db.Studios.Include(a => a.Animes).FirstOrDefault(s => s.StudioId == studioVM.StudioID);
        if (studio == null)
        {
            return RedirectToAction("Management");
        }
        if (db.Studios.Any(studio => studio.Name.Equals(studioVM.Name) && studio.StudioId != studioVM.StudioID))
        {
            ModelState.AddModelError("StudioVM.Name", "This studio already exists.");
        }
        if(studioVM.DateEstablished > DateTime.Today.ToDateOnly())
        {
            ModelState.AddModelError("StudioVM.DateEstablished", "The date cannot be in the future.");
        }
        if (studioVM.StudioImage == null)
        {
            ModelState.AddModelError("StudioVM.StudioImage", "An image of the studio's logo is required.");
        }
        else
        {
            var e = hp.ValidatePhoto(studioVM.StudioImage);
            if (e != "")
            {
                ModelState.AddModelError("StudioVM.StudioImage", e);
            }
        }
        if (ModelState.IsValid)
        {
            studio.Name = studioVM.Name;
            studio.Description = studioVM.Description;
            studio.DateEstablished = studioVM.DateEstablished;
            if (studioVM.StudioImage != null) {
                hp.DeletePhoto(studio.ImageLink, "images/studio");
                studio.ImageLink = hp.SavePhoto(studioVM.StudioImage, "images/studio");
            }
            db.SaveChanges();
            TempData["Info"] = "Studio has been updated.";
            return RedirectToAction("Management");
        }
        studioVM.ImageLink = studio.ImageLink; 
        return View("ManagementDetails", studioVM);
    }
}

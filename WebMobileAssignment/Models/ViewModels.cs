using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using X.PagedList;

namespace WebMobileAssignment.Models;

#nullable disable warnings

// View Models ----------------------------------------------------------------

public class AnimeManagementVM
{
    public List<AnimeVM> AnimeVMs { get; set; } = [];

    [ValidateNever]
    public IPagedList<Anime> Animes{  get; set; }
}

public class AnimeVM
{
    public List<GenreVM> Genres { get; set; } = [];
    public int AnimeID { get; set; }
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }
    [Range(1,10000)]
    public int Episodes { get; set; }
    [StringLength(50)]
    public string Status { get; set; }
    [StringLength(2000)]
    public string? Synopsis { get; set; }
    [Url]
    public string? TrailerLink { get; set; }
    [DataType(DataType.Date)]
    public DateOnly? ReleaseDate { get; set; }
    [Required(ErrorMessage = "A studio must be selected.")]
    public int StudioId { get; set; }
    public List<IFormFile>? AnimeImageFiles { get; set; } = [];
    public List<AnimeImage> AnimeImages { get; set; } = [];
}

public class GenreVM
{
    public int GenreID { get; set; }
    public string Name { get; set; }
    public bool Selected { get; set; }
}
public class StudioManagementVM
{
    public StudioVM StudioVM{  get; set; }
    [ValidateNever]
    public IPagedList<Studio> Studios { get; set; } 
}
public class StudioVM
{
    public int StudioID { get; set; }
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }
    [StringLength(1000)]
    public string? Description { get; set; }
    [DataType(DataType.Date)]
    public DateOnly DateEstablished { get; set; }
    public IFormFile StudioImage { get; set; }
    public List<Anime> Animes { get; set; } = [];
    public string? ImageLink { get; set; }
}
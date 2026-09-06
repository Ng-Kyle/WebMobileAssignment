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
    [Required(ErrorMessage = "NameRequiredError")]
    [StringLength(100, MinimumLength = 1,ErrorMessage = "NameLengthError")]
    public string Name { get; set; }

    [Required(ErrorMessage = "EpisodesRequiredError")]
    [Range(1, 10000, ErrorMessage = "EpisodesRangeError")]
    public int Episodes { get; set; }

    [StringLength(50, ErrorMessage = "StatusLengthError")]
    [Required(ErrorMessage = "StatusRequiredError")]
    public string Status { get; set; }

    [StringLength(2000, ErrorMessage = "SynopsisLengthError")]
    public string? Synopsis { get; set; }

    [Url(ErrorMessage = "TrailerLinkInvalid")]
    public string? TrailerLink { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? ReleaseDate { get; set; }

    [Required(ErrorMessage = "StudioRequired")]
    public int StudioId { get; set; }

    public List<IFormFile>? AnimeImageFiles { get; set; } = [];
    public List<AnimeImage> AnimeImages { get; set; } = [];
}

public class GenreManagementVM
{
    [StringLength(50, MinimumLength = 1)]
    public string Name { get; set; }
    [ValidateNever]
    public IPagedList<Genre> Genres { get; set; }
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
    [Required(ErrorMessage = "NameRequiredError")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "NameLengthError")]
    public string Name { get; set; }
    [StringLength(1000, ErrorMessage = "DescriptionLengthError")]
    public string? Description { get; set; }
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "DateEstablishedRequiredError")]
    public DateOnly DateEstablished { get; set; }
    [Required(ErrorMessage = "An image of the studio's logo is required.")]
    public IFormFile StudioImage { get; set; }
    public List<Anime> Animes { get; set; } = [];
    public string? ImageLink { get; set; }
}
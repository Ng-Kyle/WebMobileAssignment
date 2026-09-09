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
    public IPagedList<Anime> Animes { get; set; }
}

public class AnimeVM
{
    public List<GenreVM> Genres { get; set; } = [];
    public int AnimeID { get; set; }

    [Required(ErrorMessage = "NameRequiredError")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "NameLengthError")]
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
    [StringLength(50, MinimumLength = 1, ErrorMessage = "NameLengthError")]
    [Required(ErrorMessage = "NameRequiredError")]
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
    public StudioVM StudioVM { get; set; }

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

public class ProgressStatsVM
{
    // High-Level Totals
    public int TotalAnimesInList { get; set; }
    public int TotalEpisodesWatched { get; set; }
    public double CompletionRatePercentage { get; set; }

    // Breakdown by Status
    public int WatchingCount { get; set; }
    public int CompletedCount { get; set; }
    public int PlanToWatchCount { get; set; }
    public int OnHoldCount { get; set; }
    public int DroppedCount { get; set; }

    // Recently Finished List
    public List<AnimeWatchList> RecentlyCompleted { get; set; } = [];
}

public class RatingStatsVM
{
    public double AverageRating { get; set; }
    public int TotalReviewsCount { get; set; }
    public int HighestRating { get; set; }
    public int LowestRating { get; set; }

    // Key: Score (1 to 10), Value: Count of reviews given for that score
    public Dictionary<int, int> RatingDistribution { get; set; } = new();

    // Top rated reviews written by the member
    public List<Review> TopRatedReviews { get; set; } = [];
}

public class PreferenceItemVM
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageRating { get; set; }
}

public class PreferencesStatsVM
{
    public List<PreferenceItemVM> TopGenres { get; set; } = [];
    public List<PreferenceItemVM> TopStudios { get; set; } = [];
}

public class AdminReviewIndexVM
{
    public IPagedList<Review> Reviews { get; set; }
    public string Search { get; set; }
    public int? SelectedRating { get; set; }
    public int? SelectedAnimeId { get; set; }
    public string Sort { get; set; }
    public string Direction { get; set; }
}

public class MemberReviewVM
{
    public int ReviewId { get; set; }

    [Required]
    public int AnimeId { get; set; }

    public string? AnimeName { get; set; }

    [Required]
    [Range(1, 10, ErrorMessage = "Rating must be between 1 and 10.")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Review text is required.")]
    [StringLength(1000, ErrorMessage = "Review cannot exceed 1000 characters.")]
    public string Text { get; set; } = string.Empty;
}
public class LoginVM
{
    [Required(ErrorMessage = "Your email is required.")] 
    [StringLength(100,ErrorMessage = "Your email cannot exceed 100 characters.")]
    [EmailAddress(ErrorMessage = "This is not a valid email address.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Your password is required.")]
    [StringLength(100, ErrorMessage = "Your password cannot exceed 100 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    [Required, StringLength(30)]
    public string UserName { get; set; }

    [Required, StringLength(100), EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Email already in use.")]
    public string Email { get; set; }

    [Required, StringLength(100, MinimumLength = 5), DataType(DataType.Password)]
    public string Password { get; set; }

    [Required, Compare("Password"), DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }

    public IFormFile? Photo { get; set; }
}

public class ResetPasswordVM
{
    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; }
}

public class UpdateProfileVM
{
    public string? Email { get; set; }

    [Required, StringLength(30)]
    public string UserName { get; set; }

    public string? ImageLink { get; set; }

    public IFormFile? Photo { get; set; }
}

public class UpdatePasswordVM
{
    [Required, DataType(DataType.Password)]
    [DisplayName("Current Password")]
    public string Current { get; set; }

    [Required, StringLength(100, MinimumLength = 5), DataType(DataType.Password)]
    [DisplayName("New Password")]
    public string New { get; set; }

    [Required, Compare("New"), DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }
}
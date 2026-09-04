namespace WebMobileAssignment.Models;

public class AnimeCardViewModel
{
    public int animeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Studio { get; set; } = string.Empty;
    public int Episodes { get; set; }
    public string Synopsis { get; set; } = string.Empty;
}
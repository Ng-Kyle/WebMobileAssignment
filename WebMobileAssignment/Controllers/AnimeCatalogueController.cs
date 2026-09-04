using Microsoft.AspNetCore.Mvc;

namespace WebMobileAssignment.Controllers
{
    public class AnimeCatalogueController : Controller
    {
        private static readonly List<AnimeCardViewModel> SampleAnimes = new()
        {
            new AnimeCardViewModel
            {
                animeId = 1,
                Title ="Frieren: Beyond Journey's End",
                ImageUrl = "/images/frieren.jpg",
                Score = 9.1,
                Studio = "Madhouse",
                Episodes = 28,
                Synopsis = "An elf mage embarks on a journey to understand humanity."
            },
            new AnimeCardViewModel
            {
                animeId = 2,
                Title = "Jujutsu Kaisen",
                ImageUrl = "/images/nanami.jpg",
                Score = 8.6,
                Studio = "MAPPA",
                Episodes = 24,
                Synopsis = "A high school student becomes a sorcerer to fight curses."
            }
        };

        public IActionResult Index()
        {
            return View(SampleAnimes);
        }

        public IActionResult Details(int id)
        {
            var anime = SampleAnimes.FirstOrDefault(a => a.animeId == id) ?? SampleAnimes[0];
            return View(anime);
        }
    }
}

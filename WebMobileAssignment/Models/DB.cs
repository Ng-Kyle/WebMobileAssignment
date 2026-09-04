using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebMobileAssignment.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Anime> Animes { get; set; }
    public DbSet<Studio> Studios { get; set; }
    public DbSet<WatchList> WatchLists { get; set; }
    public DbSet<AnimeWatchList> AnimeWatchLists { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<AnimeImage> AnimeImages { get; set; }
    public DbSet<Review> Reviews { get; set; }  
}
public class User
{
    public int UserId { get; set; }
    [MaxLength(30)]
    public string UserName { get; set; }
    [MaxLength(255)]
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role => GetType().Name;
}

public class Member : User
{
    public WatchList WatchList { get; set; }
    public List<Review> Reviews { get; set; } = [];
}

public class Admin : User
{

}

public class WatchList
{
    public int WatchListId { get; set; }
    [ForeignKey("UserId")]
    public Member Member { get; set; }
    public List<AnimeWatchList> AnimeWatchLists { get; set; } = [];
}

public class Anime
{
    public int AnimeId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [Range(1, 10000)]
    public int Episodes { get; set; }
    [MaxLength(50)]
    public string Status { get; set; }
    [MaxLength(2000)]
    public string? Synopsis { get; set; }
    public string? TrailerLink { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public List<Genre> Genres { get; set; } = [];
    public Studio Studio { get; set; }
    public List<AnimeImage>? AnimeImages { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
    public List<AnimeWatchList> AnimeWatchLists { get; set; } = [];
}

public class AnimeWatchList
{
    public int AnimeWatchListId { get; set; }
    [MaxLength(50)]
    public string Status { get; set; }
    [Range(0, 10000)]
    public int Progress { get; set; }
    public int AnimeId { get; set; }
    [ForeignKey("AnimeId")]
    public Anime Anime { get; set; }
    public int WatchListId { get; set; }
    [ForeignKey("WatchListId")]
    public WatchList WatchList { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public class Studio
{
    public int StudioId { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [MaxLength(1000)]
    public string? Description { get; set; }
    public DateOnly DateEstablished { get; set; }
    public List<Anime> Animes { get; set; } = [];
    public string ImageLink { get; set; }
}
public class Genre
{
    public int GenreId { get; set; }
    [MaxLength(50)]
    public string Name { get; set; }
    public List<Anime> Animes { get; set; } = [];
}

public class AnimeImage
{
    public int AnimeImageId { get; set; }
    public string ImageLink { get; set; }
    public Anime Anime { get; set; }
    public bool IsThumbnail { get; set; }
    [Range(1, 20)]
    public int DisplayOrder { get; set; }
}

public class Review
{
    public int ReviewId { get; set; }
    [Range(1, 10)]
    public int Rating { get; set; }
    [MaxLength(1000)]
    public string Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Member Member { get; set; }
    public Anime Anime { get; set; }
}
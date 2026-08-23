using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Story> Stories { get; set; } = null!;
    public DbSet<StoryGenre> StoryGenres { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Define relationship
        modelBuilder.Entity<Story>()
            .HasOne(s => s.Genre)
            .WithMany(g => g.Stories)
            .HasForeignKey(s => s.GenreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional: seed defaults (also handled manually at startup below)
        modelBuilder.Entity<StoryGenre>().HasData(
            new StoryGenre { StoryGenreId = 1, Name = "Science Fiction" },
            new StoryGenre { StoryGenreId = 2, Name = "Historical Fiction" }
        );
    }
}

using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Story> Stories { get; set; } = null!;
    public DbSet<StoryGenre> StoryGenres { get; set; } = null!;
    public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; } = null!;
    public DbSet<NarrativeTransportationEvaluation> NarrativeTransportationEvaluations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Define relationship
        modelBuilder.Entity<Story>()
            .HasOne(s => s.Genre)
            .WithMany(g => g.Stories)
            .HasForeignKey(s => s.GenreId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<KnowledgeChunk>()
            .HasOne(k => k.Genre)
            .WithMany()
            .HasForeignKey(k => k.GenreId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<NarrativeTransportationEvaluation>()
            .HasOne(e => e.Story)
            .WithMany()
            .HasForeignKey(e => e.StoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional: seed defaults (also handled manually at startup below)
        modelBuilder.Entity<StoryGenre>().HasData(
            new StoryGenre { StoryGenreId = 1, Name = "Science Fiction" },
            new StoryGenre { StoryGenreId = 2, Name = "Historical Fiction" },
            new StoryGenre { StoryGenreId = 3, Name = "Mystery" },
            new StoryGenre { StoryGenreId = 4, Name = "Fantasy" },
            new StoryGenre { StoryGenreId = 5, Name = "Horror" },
            new StoryGenre { StoryGenreId = 6, Name = "Dystopian" }
        );
    }
}

public class StoryGenre
{
    public int StoryGenreId { get; set; }
    public string Name { get; set; } = null!;
    public List<Story> Stories { get; set; } = new();
}

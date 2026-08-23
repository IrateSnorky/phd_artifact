public class Story
{
    public int StoryId { get; set; }
    public string? StoryInstructions { get; set; }
    public string? StoryPrompt { get; set; }
    public string? GeneratedStory { get; set; }

    // Link to StoryGenre
    public int? GenreId { get; set; }
    public StoryGenre? Genre { get; set; }
}

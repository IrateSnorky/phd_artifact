public class KnowledgeChunk
{
    public int KnowledgeChunkId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Source { get; set; }

    // When true, this chunk is injected into every applicable story generation prompt as a
    // non-negotiable rule, regardless of similarity to the story's prompt.
    public bool AlwaysInclude { get; set; }

    // When set, this chunk only applies to stories of this genre. Null means it applies to all genres.
    public int? GenreId { get; set; }
    public StoryGenre? Genre { get; set; }

    // JSON-serialized float array produced by the Gemini embedding model.
    public string? Embedding { get; set; }
}

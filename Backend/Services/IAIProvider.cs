namespace Backend.Services;

/// <summary>
/// Abstract interface for AI providers to decouple from Gemini-specific implementation.
/// Supports story generation, transformation, embeddings, and scoring.
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Generates a story based on prompt, instructions, guardrails, and context.
    /// </summary>
    Task<string> GenerateStoryAsync(
        string prompt,
        string instructions,
        List<string> guardrails,
        string retrievedContext,
        string genre);

    /// <summary>
    /// Transforms a story to fit a specific office setting while preserving original content.
    /// </summary>
    Task<string> TransformStoryAsync(
        string sourceStory,
        string officeName,
        string officeDescription);

    /// <summary>
    /// Improves a transformed story using temporary feedback-derived guardrails.
    /// </summary>
    Task<string> ImproveStoryAsync(
        string currentStory,
        List<string> guardrails,
        string officeName,
        string officeDescription);

    /// <summary>
    /// Generates a vector embedding for semantic similarity search.
    /// </summary>
    Task<float[]?> GetEmbeddingAsync(string text);

    /// <summary>
    /// Evaluates a story using the Narrative Transportation Scale (9-63 points).
    /// </summary>
    Task<int?> EvaluateNarrativeTransportationScoreAsync(string storyText);
}

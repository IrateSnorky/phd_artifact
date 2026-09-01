namespace Backend.Services;

using System.Text.Json;

/// <summary>
/// Google Gemini implementation of IAIProvider.
/// Handles story generation, transformation, embeddings, and narrative transportation scoring.
/// </summary>
public class GeminiProvider : IAIProvider
{
    public string Name => "gemini";

    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string GenerationModel = "gemini-3.5-flash";
    private const string EmbeddingModel = "gemini-embedding-001";
    private const string BaseUrl = "https://generativelanguage.googleapis.com";

    public GeminiProvider(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateStoryAsync(
        string prompt,
        string instructions,
        List<string> guardrails,
        string retrievedContext,
        string genre)
    {
        var promptBuilder = new System.Text.StringBuilder();
        if (guardrails.Count > 0)
        {
            promptBuilder.AppendLine("Story guardrails (must always be followed, no exceptions):");
            foreach (var guardrail in guardrails)
                promptBuilder.AppendLine($"- {guardrail}");
            promptBuilder.AppendLine();
        }
        promptBuilder.AppendLine("Generate a single paragraph story (2-3 sentences) based on these inputs:");
        promptBuilder.AppendLine($"- Genre: {genre}");
        promptBuilder.AppendLine($"- Instructions: {instructions}");
        promptBuilder.AppendLine($"- Prompt: {prompt}");
        if (!string.IsNullOrEmpty(retrievedContext))
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Relevant reference context (use for consistency if applicable):");
            promptBuilder.AppendLine(retrievedContext);
        }
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Write only the story paragraph, nothing else.");

        var finalPrompt = promptBuilder.ToString();
        return await CallGenerationApiAsync(finalPrompt);
    }

    public async Task<string> TransformStoryAsync(
        string sourceStory,
        string officeName,
        string officeDescription)
    {
        var prompt = $"""
            Rewrite the story below so its backdrop naturally takes place in this office setting.
            Preserve the story's plot, characters, tone, and approximate length. Change only details
            needed to make the setting feel integral to the story. Return only the rewritten story.

            Office setting: {officeName}
            Office context: {officeDescription}

            Story:
            {sourceStory}
            """;

        return await CallGenerationApiAsync(prompt);
    }

    public async Task<string> ImproveStoryAsync(
        string currentStory,
        List<string> guardrails,
        string officeName,
        string officeDescription)
    {
        var guardrailText = string.Join(Environment.NewLine, guardrails.Select(guardrail => $"- {guardrail}"));
        var prompt = $"""
            Improve the story below for reader engagement while preserving its plot, characters,
            tone, office setting, and approximate length. Apply every improvement guardrail.
            Return only the improved story.

            Office setting: {officeName}
            Office context: {officeDescription}

            Improvement guardrails:
            {guardrailText}

            Story:
            {currentStory}
            """;

        return await CallGenerationApiAsync(prompt);
    }

    public async Task<float[]?> GetEmbeddingAsync(string text)
    {
        var requestBody = new
        {
            content = new { parts = new[] { new { text } } }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/v1beta/models/{EmbeddingModel}:embedContent?key={_apiKey}",
            jsonContent);

        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var values = doc.RootElement.GetProperty("embedding").GetProperty("values");
        return values.EnumerateArray().Select(v => v.GetSingle()).ToArray();
    }

    public async Task<int?> EvaluateNarrativeTransportationScoreAsync(string storyText)
    {
        var items = new[]
        {
            "While I was reading the narrative, I could easily picture the events taking place.",
            "I was mentally involved in the narrative while reading it.",
            "The narrative affected me emotionally.",
            "I found myself thinking of ways the narrative could have turned out differently.",
            "While reading the narrative I had a vivid image of the characters.",
            "I wanted to learn how the narrative ended.",
            "I found my mind wandering while reading the narrative.",
            "The events in the narrative are relevant to my everyday life.",
            "The narrative changed my understanding of things."
        };

        var prompt = string.Join(
            Environment.NewLine,
            "Evaluate the following story using the Narrative Transportation scale.",
            "Score each of these statements from 1 (not at all) to 5 (very much):",
            string.Empty,
            string.Join(Environment.NewLine, items.Select((item, index) => $"{index + 1}. {item}")),
            string.Empty,
            "Important: For the reverse-scored item (\"I found my mind wandering while reading the narrative.\"), compute the score as 6 - response so that higher values mean greater transportation.",
            string.Empty,
            "Return only valid JSON in this exact shape:",
            "{ \"total\": 0, \"average\": 0.0 }",
            "Use the total score across all items, with total between 15 and 75, and average between 1.0 and 5.0.",
            string.Empty,
            "Story:",
            storyText
        );

        var responseText = await CallGenerationApiAsync(prompt);
        if (string.IsNullOrWhiteSpace(responseText)) return null;

        var jsonStart = responseText.IndexOf('{');
        var jsonEnd = responseText.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd < jsonStart) return null;

        var json = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
        using var scoreDoc = JsonDocument.Parse(json);
        if (!scoreDoc.RootElement.TryGetProperty("total", out var totalElement)) return null;

        var total = totalElement.GetInt32();
        return Math.Clamp(total, 15, 75);
    }

    private async Task<string> CallGenerationApiAsync(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/v1/models/{GenerationModel}:generateContent?key={_apiKey}",
            jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gemini API error: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        var generatedText = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(generatedText))
            throw new InvalidOperationException("Gemini returned an empty response");

        return generatedText;
    }
}

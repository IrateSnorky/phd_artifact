namespace Backend.Services;

using System.Text.Json;

/// <summary>
/// Cohere implementation of IAIProvider.
/// Handles story generation, transformation, embeddings, and narrative transportation scoring.
/// Cohere is ideal for guardrails and knowledge base grounding.
/// </summary>
public class CohereProvider : IAIProvider
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.cohere.com";
    private const string GenerationModel = "command-r-plus-08-2024";
    private const string EmbeddingModel = "embed-english-v3.0";

    public CohereProvider(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + _apiKey);
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
        var promptBuilder = new System.Text.StringBuilder();
        promptBuilder.AppendLine("Improve the story below for reader engagement while preserving its plot, characters, tone, office setting, and approximate length.");
        promptBuilder.AppendLine("Apply every improvement guardrail and return only the improved story.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Office setting: {officeName}");
        promptBuilder.AppendLine($"Office context: {officeDescription}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Improvement guardrails:");
        foreach (var guardrail in guardrails)
            promptBuilder.AppendLine($"- {guardrail}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Story:");
        promptBuilder.AppendLine(currentStory);

        return await CallGenerationApiAsync(promptBuilder.ToString());
    }

    public async Task<float[]?> GetEmbeddingAsync(string text)
    {
        var requestBody = new
        {
            texts = new[] { text },
            model = EmbeddingModel,
            input_type = "search_document",
            embedding_types = new[] { "float" }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/v2/embed",
            jsonContent);

        if (!response.IsSuccessStatusCode) return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        
        var embeddings = doc.RootElement.GetProperty("embeddings");
        JsonElement firstEmbedding;
        if (embeddings.ValueKind == JsonValueKind.Object &&
            embeddings.TryGetProperty("float", out var floatEmbeddings))
        {
            if (floatEmbeddings.GetArrayLength() == 0) return null;
            firstEmbedding = floatEmbeddings[0];
        }
        else
        {
            if (embeddings.GetArrayLength() == 0) return null;
            firstEmbedding = embeddings[0];
        }

        return firstEmbedding.EnumerateArray()
            .Select(v => v.GetSingle())
            .ToArray();
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
            "Score each of these statements from 1 (not at all) to 7 (very much):",
            string.Empty,
            string.Join(Environment.NewLine, items.Select((item, index) => $"{index + 1}. {item}")),
            string.Empty,
            "Important: For the reverse-scored item (\"I found my mind wandering while reading the narrative.\"), compute the score as 8 - response so that higher values mean greater transportation.",
            string.Empty,
            "Return only valid JSON in this exact shape:",
            "{ \"total\": 0, \"average\": 0.0 }",
            "Use the total score across all items, with total between 9 and 63, and average between 1.0 and 7.0.",
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
        return Math.Clamp(total, 9, 63);
    }

    private async Task<string> CallGenerationApiAsync(string prompt)
    {
        var requestBody = new
        {
            model = GenerationModel,
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 1024,
            temperature = 0.8
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            $"{BaseUrl}/v2/chat",
            jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Cohere API error: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        
        string? generatedText = null;
        if (doc.RootElement.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content))
        {
            generatedText = content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0
                ? content[0].GetProperty("text").GetString()
                : content.GetString();
        }
        else if (doc.RootElement.TryGetProperty("generations", out var generations) &&
                 generations.GetArrayLength() > 0)
        {
            generatedText = generations[0].GetProperty("text").GetString();
        }

        if (string.IsNullOrWhiteSpace(generatedText))
            throw new InvalidOperationException("Cohere returned no generated text");

        return generatedText;
    }
}

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- ADD THIS BLOCK FOR CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
// -------------------------------

// Add EF Core SQLite DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=stories.db"));

builder.Services.AddOpenApi();

var app = builder.Build();

// Create the database when absent and seed genres without overwriting saved stories.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();

    // EnsureCreated() only builds the schema for brand-new databases, so tables added after
    // the database already existed (e.g. KnowledgeChunks) must be created explicitly here.
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "KnowledgeChunks" (
            "KnowledgeChunkId" INTEGER NOT NULL CONSTRAINT "PK_KnowledgeChunks" PRIMARY KEY AUTOINCREMENT,
            "Content" TEXT NOT NULL,
            "Source" TEXT NULL,
            "Embedding" TEXT NULL
        );
        """);

    // Likewise, columns added to an existing table (e.g. AlwaysInclude for guardrails)
    // must be applied manually since EnsureCreated() will not alter existing tables.
    var hasAlwaysIncludeColumn = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('KnowledgeChunks') WHERE name = 'AlwaysInclude'"
    ).AsEnumerable().First() > 0;
    if (!hasAlwaysIncludeColumn)
    {
        db.Database.ExecuteSqlRaw(
            "ALTER TABLE \"KnowledgeChunks\" ADD COLUMN \"AlwaysInclude\" INTEGER NOT NULL DEFAULT 0;"
        );
    }

    var hasGenreIdColumn = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('KnowledgeChunks') WHERE name = 'GenreId'"
    ).AsEnumerable().First() > 0;
    if (!hasGenreIdColumn)
    {
        db.Database.ExecuteSqlRaw(
            "ALTER TABLE \"KnowledgeChunks\" ADD COLUMN \"GenreId\" INTEGER NULL;"
        );
    }

    var hasNarrativeScoreColumn = db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) AS \"Value\" FROM pragma_table_info('Stories') WHERE name = 'NarrativeTransportationScore'"
    ).AsEnumerable().First() > 0;
    if (!hasNarrativeScoreColumn)
    {
        db.Database.ExecuteSqlRaw(
            "ALTER TABLE \"Stories\" ADD COLUMN \"NarrativeTransportationScore\" INTEGER NULL;"
        );
    }

    if (!db.StoryGenres.Any())
    {
        db.StoryGenres.AddRange(new[] {
            new StoryGenre { StoryGenreId = 1, Name = "Science Fiction" },
            new StoryGenre { StoryGenreId = 2, Name = "Historical Fiction" }
        });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// --- ADD THIS LINE TO ACTIVATE CORS ---
app.UseCors("AllowReact");
// -------------------------------------

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Genres endpoint
app.MapGet("/genres", async (AppDbContext db) =>
    await db.StoryGenres.Select(g => new { id = g.StoryGenreId, name = g.Name }).ToListAsync());

// Stories endpoints
app.MapGet("/stories", async (AppDbContext db) =>
    await db.Stories
        .Select(s => new
        {
            storyId = s.StoryId,
            storyInstructions = s.StoryInstructions,
            storyPrompt = s.StoryPrompt,
            generatedStory = s.GeneratedStory,
            narrativeTransportationScore = s.NarrativeTransportationScore,
            genreId = s.GenreId,
            genreName = s.Genre != null ? s.Genre.Name : null
        })
        .ToListAsync());

app.MapPost("/stories", async (Story story, AppDbContext db) =>
{
    // Accept genre ID if provided
    db.Stories.Add(story);
    await db.SaveChangesAsync();
    return Results.Created($"/stories/{story.StoryId}", story);
});

app.MapPut("/stories/{id}", async (int id, Story input, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();
    story.StoryInstructions = input.StoryInstructions;
    story.StoryPrompt = input.StoryPrompt;
    story.GenreId = input.GenreId;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/stories/{id}", async (int id, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();
    db.Stories.Remove(story);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/stories/{id}/narrative-transportation", async (int id, NarrativeTransportationSurveyRequest request, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();

    var responses = request.Responses ?? Array.Empty<int>();
    if (responses.Length != 15)
        return Results.BadRequest("Exactly 15 response values are required.");

    if (responses.Any(r => r < 1 || r > 7))
        return Results.BadRequest("Each response must be between 1 and 7.");

    var total = 0;
    for (var i = 0; i < responses.Length; i++)
    {
        var value = responses[i];
        total += i == 6 ? 8 - value : value;
    }

    story.NarrativeTransportationScore = total;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        narrativeTransportationScore = total,
        average = total / 15.0,
        maxScore = 105,
        itemCount = 15,
    });
});

app.MapPost("/stories/{id}/transform-for-office", async (int id, OfficeStoryTransformRequest input, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(input.OfficeName) || string.IsNullOrWhiteSpace(input.OfficeDescription))
        return Results.BadRequest("Office name and description are required");

    var sourceStory = story.GeneratedStory ?? story.StoryPrompt ?? story.StoryInstructions;
    if (string.IsNullOrWhiteSpace(sourceStory))
        return Results.BadRequest("This story does not have content to transform");

    var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (string.IsNullOrEmpty(geminiApiKey))
        return Results.BadRequest("GEMINI_API_KEY environment variable not set");

    var prompt = $"""
        Rewrite the story below so its backdrop naturally takes place in this office setting.
        Preserve the story's plot, characters, tone, and approximate length. Change only details
        needed to make the setting feel integral to the story. Return only the rewritten story.

        Office setting: {input.OfficeName}
        Office context: {input.OfficeDescription}

        Story:
        {sourceStory}
        """;

    using var httpClient = new HttpClient();
    var requestBody = new
    {
        contents = new[]
        {
            new { parts = new[] { new { text = prompt } } }
        }
    };
    var jsonContent = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(requestBody),
        System.Text.Encoding.UTF8,
        "application/json");
    var response = await httpClient.PostAsync(
        $"https://generativelanguage.googleapis.com/v1/models/gemini-3.5-flash:generateContent?key={geminiApiKey}",
        jsonContent);

    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        return Results.BadRequest($"Gemini API error: {response.StatusCode} - {errorContent}");
    }

    using var responseDocument = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var transformedStory = responseDocument.RootElement
        .GetProperty("candidates")[0]
        .GetProperty("content")
        .GetProperty("parts")[0]
        .GetProperty("text")
        .GetString();

    return string.IsNullOrWhiteSpace(transformedStory)
        ? Results.BadRequest("Gemini returned an empty transformed story")
        : Results.Ok(new { transformedStory });
});

// Knowledge base endpoints (used to retrieve reference context for story generation)
app.MapGet("/knowledge", async (AppDbContext db) =>
    await db.KnowledgeChunks
        .Select(k => new { id = k.KnowledgeChunkId, content = k.Content, source = k.Source, alwaysInclude = k.AlwaysInclude, genreId = k.GenreId, genreName = k.Genre != null ? k.Genre.Name : null })
        .ToListAsync());

app.MapPost("/knowledge", async (KnowledgeRequest input, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Content))
        return Results.BadRequest("Content is required");

    var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (string.IsNullOrEmpty(geminiApiKey))
        return Results.BadRequest("GEMINI_API_KEY environment variable not set");

    // Guardrails apply as whole documents (e.g. "no graphic violence"), so they are not
    // split into paragraphs the way retrievable reference lore is.
    var paragraphs = input.AlwaysInclude
        ? new List<string> { input.Content.Trim() }
        : input.Content
            .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
    if (paragraphs.Count == 0) paragraphs.Add(input.Content.Trim());

    using var httpClient = new HttpClient();
    var created = new List<KnowledgeChunk>();

    foreach (var paragraph in paragraphs)
    {
        var embedding = await GetEmbeddingAsync(httpClient, geminiApiKey, paragraph);
        if (embedding is null)
            return Results.BadRequest("Failed to generate embedding for one or more chunks");

        var chunk = new KnowledgeChunk
        {
            Content = paragraph,
            Source = input.Source,
            AlwaysInclude = input.AlwaysInclude,
            GenreId = input.GenreId,
            Embedding = System.Text.Json.JsonSerializer.Serialize(embedding)
        };
        db.KnowledgeChunks.Add(chunk);
        created.Add(chunk);
    }

    await db.SaveChangesAsync();
    return Results.Created("/knowledge", created.Select(c => new { id = c.KnowledgeChunkId, content = c.Content, source = c.Source, alwaysInclude = c.AlwaysInclude, genreId = c.GenreId }));
});

app.MapPut("/knowledge/{id}", async (int id, KnowledgeRequest input, AppDbContext db) =>
{
    var chunk = await db.KnowledgeChunks.FindAsync(id);
    if (chunk is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(input.Content))
        return Results.BadRequest("Content is required");

    var trimmedContent = input.Content.Trim();
    var contentChanged = trimmedContent != chunk.Content;

    if (contentChanged)
    {
        var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrEmpty(geminiApiKey))
            return Results.BadRequest("GEMINI_API_KEY environment variable not set");

        using var httpClient = new HttpClient();
        var embedding = await GetEmbeddingAsync(httpClient, geminiApiKey, trimmedContent);
        if (embedding is null)
            return Results.BadRequest("Failed to generate embedding");
        chunk.Embedding = System.Text.Json.JsonSerializer.Serialize(embedding);
    }

    chunk.Content = trimmedContent;
    chunk.Source = input.Source;
    chunk.AlwaysInclude = input.AlwaysInclude;
    chunk.GenreId = input.GenreId;

    await db.SaveChangesAsync();
    return Results.Ok(new { id = chunk.KnowledgeChunkId, content = chunk.Content, source = chunk.Source, alwaysInclude = chunk.AlwaysInclude, genreId = chunk.GenreId });
});

app.MapDelete("/knowledge/{id}", async (int id, AppDbContext db) =>
{
    var chunk = await db.KnowledgeChunks.FindAsync(id);
    if (chunk is null) return Results.NotFound();
    db.KnowledgeChunks.Remove(chunk);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Generate story endpoint using Google Gemini, augmented with RAG over the knowledge base
app.MapPost("/stories/{id}/generate", async (int id, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();

    var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (string.IsNullOrEmpty(geminiApiKey))
        return Results.BadRequest("GEMINI_API_KEY environment variable not set");

    var genre = story.GenreId.HasValue ? (await db.StoryGenres.FindAsync(story.GenreId))?.Name : "General";

    // Guardrails always apply when generating a story of a matching genre (or genre-less guardrails, which apply to all).
    var guardrails = await db.KnowledgeChunks
        .Where(k => k.AlwaysInclude && (k.GenreId == null || k.GenreId == story.GenreId))
        .Select(k => k.Content)
        .ToListAsync();

    // Retrieve relevant (non-guardrail) knowledge base chunks for this story's prompt/instructions.
    var retrievedContext = "";
    using (var embedClient = new HttpClient())
    {
        var queryText = $"{story.StoryPrompt} {story.StoryInstructions}".Trim();
        var queryEmbedding = string.IsNullOrEmpty(queryText) ? null : await GetEmbeddingAsync(embedClient, geminiApiKey, queryText);

        if (queryEmbedding is not null)
        {
            var chunks = await db.KnowledgeChunks.Where(k => !k.AlwaysInclude).ToListAsync();
            var topMatches = chunks
                .Select(c => new
                {
                    Chunk = c,
                    Score = CosineSimilarity(queryEmbedding, DeserializeEmbedding(c.Embedding))
                })
                .Where(x => x.Score > 0.5)
                .OrderByDescending(x => x.Score)
                .Take(3)
                .ToList();

            if (topMatches.Count > 0)
                retrievedContext = string.Join("\n---\n", topMatches.Select(x => x.Chunk.Content));
        }
    }

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
    promptBuilder.AppendLine($"- Instructions: {story.StoryInstructions}");
    promptBuilder.AppendLine($"- Prompt: {story.StoryPrompt}");
    if (!string.IsNullOrEmpty(retrievedContext))
    {
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Relevant reference context (use for consistency if applicable):");
        promptBuilder.AppendLine(retrievedContext);
    }
    promptBuilder.AppendLine();
    promptBuilder.AppendLine("Write only the story paragraph, nothing else.");

    var prompt = promptBuilder.ToString();

    try
    {
        using (var httpClient = new HttpClient())
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1/models/gemini-3.5-flash:generateContent?key={geminiApiKey}",
                jsonContent
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Results.BadRequest($"Gemini API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using (var doc = System.Text.Json.JsonDocument.Parse(responseContent))
            {
                var root = doc.RootElement;
                var generatedText = root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                story.GeneratedStory = generatedText;

                try
                {
                    using var scoreClient = new HttpClient();
                    story.NarrativeTransportationScore = await EvaluateNarrativeTransportationScoreAsync(
                        scoreClient,
                        geminiApiKey,
                        generatedText
                    );
                }
                catch
                {
                    story.NarrativeTransportationScore = null;
                }

                await db.SaveChangesAsync();

                return Results.Ok(new { generatedStory = generatedText, narrativeTransportationScore = story.NarrativeTransportationScore });
            }
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error: {ex.Message}");
    }
});

app.Run();

// Calls Gemini's embedding model to convert text into a vector for similarity search.
static async Task<float[]?> GetEmbeddingAsync(HttpClient client, string apiKey, string text)
{
    var requestBody = new
    {
        content = new { parts = new[] { new { text } } }
    };

    var jsonContent = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(requestBody),
        System.Text.Encoding.UTF8,
        "application/json"
    );

    var response = await client.PostAsync(
        $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={apiKey}",
        jsonContent
    );

    if (!response.IsSuccessStatusCode) return null;

    var responseContent = await response.Content.ReadAsStringAsync();
    using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
    var values = doc.RootElement.GetProperty("embedding").GetProperty("values");
    return values.EnumerateArray().Select(v => v.GetSingle()).ToArray();
}

static async Task<int?> EvaluateNarrativeTransportationScoreAsync(HttpClient client, string apiKey, string storyText)
{
    // This implements the 1-7 Likert scale described by the user.
    // The reverse-scored item is the mind-wandering item, which is converted to a positive score before summing.
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
        "Return only valid JSON in this exact shape:",
        "{ \"total\": 0, \"average\": 0.0 }",
        "Use the total score across all items, with total between 9 and 63, and average between 1.0 and 7.0.",
        string.Empty,
        "Story:",
        storyText
    );

    var requestBody = new
    {
        contents = new[]
        {
            new { parts = new[] { new { text = prompt } } }
        }
    };

    var jsonContent = new StringContent(
        System.Text.Json.JsonSerializer.Serialize(requestBody),
        System.Text.Encoding.UTF8,
        "application/json"
    );

    var response = await client.PostAsync(
        $"https://generativelanguage.googleapis.com/v1/models/gemini-3.5-flash:generateContent?key={apiKey}",
        jsonContent
    );

    if (!response.IsSuccessStatusCode) return null;

    var responseContent = await response.Content.ReadAsStringAsync();
    using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
    var rawText = doc.RootElement
        .GetProperty("candidates")[0]
        .GetProperty("content")
        .GetProperty("parts")[0]
        .GetProperty("text")
        .GetString();

    if (string.IsNullOrWhiteSpace(rawText)) return null;

    var jsonStart = rawText.IndexOf('{');
    var jsonEnd = rawText.LastIndexOf('}');
    if (jsonStart < 0 || jsonEnd < jsonStart) return null;

    var json = rawText.Substring(jsonStart, jsonEnd - jsonStart + 1);
    using var scoreDoc = System.Text.Json.JsonDocument.Parse(json);
    if (!scoreDoc.RootElement.TryGetProperty("total", out var totalElement)) return null;

    var total = totalElement.GetInt32();
    return Math.Clamp(total, 9, 63);
}

static float[] DeserializeEmbedding(string? json) =>
    string.IsNullOrEmpty(json) ? Array.Empty<float>() : System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();

static double CosineSimilarity(float[] a, float[] b)
{
    if (a.Length == 0 || b.Length == 0 || a.Length != b.Length) return 0;

    double dot = 0, magnitudeA = 0, magnitudeB = 0;
    for (var i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        magnitudeA += a[i] * a[i];
        magnitudeB += b[i] * b[i];
    }

    if (magnitudeA == 0 || magnitudeB == 0) return 0;
    return dot / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
}

record KnowledgeRequest(string Content, string? Source, bool AlwaysInclude = false, int? GenreId = null);

record NarrativeTransportationSurveyRequest(int[] Responses);

record OfficeStoryTransformRequest(string OfficeName, string OfficeDescription);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

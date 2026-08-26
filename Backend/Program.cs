using Microsoft.EntityFrameworkCore;
using Backend.Services;

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

    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "NarrativeTransportationEvaluations" (
            "NarrativeTransportationEvaluationId" INTEGER NOT NULL CONSTRAINT "PK_NarrativeTransportationEvaluations" PRIMARY KEY AUTOINCREMENT,
            "StoryId" INTEGER NOT NULL,
            "ResponsesJson" TEXT NOT NULL,
            "AdjustedResponsesJson" TEXT NOT NULL,
            "TransformedStory" TEXT NOT NULL,
            "OfficeName" TEXT NOT NULL,
            "OfficeDescription" TEXT NOT NULL,
            "SubmittedAtUtc" TEXT NOT NULL,
            "StoryVersion" TEXT NOT NULL,
            "TotalScore" INTEGER NOT NULL,
            CONSTRAINT "FK_NarrativeTransportationEvaluations_Stories_StoryId" FOREIGN KEY ("StoryId") REFERENCES "Stories" ("StoryId") ON DELETE CASCADE
        );
        """);

    var defaultGenres = new[]
    {
        new StoryGenre { StoryGenreId = 1, Name = "Science Fiction" },
        new StoryGenre { StoryGenreId = 2, Name = "Historical Fiction" },
        new StoryGenre { StoryGenreId = 3, Name = "Mystery" },
        new StoryGenre { StoryGenreId = 4, Name = "Fantasy" },
        new StoryGenre { StoryGenreId = 5, Name = "Horror" },
        new StoryGenre { StoryGenreId = 6, Name = "Dystopian" }
    };
    foreach (var genre in defaultGenres)
    {
        if (!db.StoryGenres.Any(existing => existing.StoryGenreId == genre.StoryGenreId))
        {
            db.StoryGenres.Add(genre);
        }
    }
    if (db.ChangeTracker.HasChanges())
    {
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
    if (string.IsNullOrWhiteSpace(request.TransformedStory) ||
        string.IsNullOrWhiteSpace(request.OfficeName) ||
        string.IsNullOrWhiteSpace(request.StoryVersion))
        return Results.BadRequest("Transformed story, office name, and story version are required.");

    var responses = request.Responses ?? Array.Empty<int>();
    if (responses.Length != 15)
        return Results.BadRequest("Exactly 15 response values are required.");

    if (responses.Any(r => r < 1 || r > 7))
        return Results.BadRequest("Each response must be between 1 and 7.");

    var adjustedResponses = new int[responses.Length];
    var total = 0;
    for (var i = 0; i < responses.Length; i++)
    {
        adjustedResponses[i] = i == 6 ? 8 - responses[i] : responses[i];
        total += adjustedResponses[i];
    }

    story.NarrativeTransportationScore = total;
    var evaluation = new NarrativeTransportationEvaluation
    {
        StoryId = story.StoryId,
        ResponsesJson = System.Text.Json.JsonSerializer.Serialize(responses),
        AdjustedResponsesJson = System.Text.Json.JsonSerializer.Serialize(adjustedResponses),
        TransformedStory = request.TransformedStory,
        OfficeName = request.OfficeName,
        OfficeDescription = request.OfficeDescription,
        SubmittedAtUtc = DateTime.UtcNow,
        StoryVersion = request.StoryVersion,
        TotalScore = total,
    };
    db.NarrativeTransportationEvaluations.Add(evaluation);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        narrativeTransportationScore = total,
        average = total / 15.0,
        maxScore = 105,
        itemCount = 15,
        evaluationId = evaluation.NarrativeTransportationEvaluationId,
        storyVersion = evaluation.StoryVersion,
        submittedAtUtc = evaluation.SubmittedAtUtc,
        responses,
        adjustedResponses,
    });
});

app.MapGet("/stories/{id}/narrative-transportation", async (int id, AppDbContext db) =>
    await db.NarrativeTransportationEvaluations
        .Where(e => e.StoryId == id)
        .OrderByDescending(e => e.SubmittedAtUtc)
        .Select(e => new
        {
            evaluationId = e.NarrativeTransportationEvaluationId,
            storyId = e.StoryId,
            responses = e.ResponsesJson,
            adjustedResponses = e.AdjustedResponsesJson,
            transformedStory = e.TransformedStory,
            officeName = e.OfficeName,
            officeDescription = e.OfficeDescription,
            submittedAtUtc = e.SubmittedAtUtc,
            storyVersion = e.StoryVersion,
            totalScore = e.TotalScore,
        })
        .ToListAsync());

app.MapGet("/feedback-insights", async (AppDbContext db) =>
{
    var evaluations = await db.NarrativeTransportationEvaluations
        .Select(e => new { e.ResponsesJson, e.AdjustedResponsesJson })
        .ToListAsync();

    return Results.Ok(BuildFeedbackInsights(evaluations));
});

app.MapPost("/feedback-insights/{category}/knowledge", async (string category, HttpRequest request, AppDbContext db) =>
{
    var evaluations = await db.NarrativeTransportationEvaluations
        .Select(e => new { e.ResponsesJson, e.AdjustedResponsesJson })
        .ToListAsync();
    var insight = BuildFeedbackInsights(evaluations)
        .FirstOrDefault(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));
    if (insight is null) return Results.NotFound("No repeated improvement pattern exists for this category.");
    var guidance = insight.Guidance!;

    if (!TryResolveProvider(request, out var provider, out var providerError))
        return Results.BadRequest(providerError);

    var embedding = await provider!.GetEmbeddingAsync(guidance);
    if (embedding is null) return Results.BadRequest("Failed to generate embedding for the feedback guidance");

    var chunk = new KnowledgeChunk
    {
        Content = guidance,
        Source = $"Narrative transportation feedback ({insight.EvaluationCount} evaluations)",
        AlwaysInclude = false,
        Embedding = System.Text.Json.JsonSerializer.Serialize(embedding),
    };
    db.KnowledgeChunks.Add(chunk);
    await db.SaveChangesAsync();
    return Results.Created($"/knowledge/{chunk.KnowledgeChunkId}", new { id = chunk.KnowledgeChunkId, content = chunk.Content });
});

app.MapPost("/stories/{id}/transform-for-office", async (int id, OfficeStoryTransformRequest input, HttpRequest request, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(input.OfficeName) || string.IsNullOrWhiteSpace(input.OfficeDescription))
        return Results.BadRequest("Office name and description are required");

    var sourceStory = story.GeneratedStory ?? story.StoryPrompt ?? story.StoryInstructions;
    if (string.IsNullOrWhiteSpace(sourceStory))
        return Results.BadRequest("This story does not have content to transform");

    if (!TryResolveProvider(request, out var provider, out var providerError))
        return Results.BadRequest(providerError);

    try
    {
        var transformedStory = await provider!.TransformStoryAsync(
            sourceStory,
            input.OfficeName,
            input.OfficeDescription);
        return Results.Ok(new { transformedStory, storyVersion = Guid.NewGuid().ToString("N") });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Knowledge base endpoints (used to retrieve reference context for story generation)
app.MapGet("/knowledge", async (AppDbContext db) =>
    await db.KnowledgeChunks
        .Select(k => new { id = k.KnowledgeChunkId, content = k.Content, source = k.Source, alwaysInclude = k.AlwaysInclude, genreId = k.GenreId, genreName = k.Genre != null ? k.Genre.Name : null })
        .ToListAsync());

app.MapPost("/knowledge", async (KnowledgeRequest input, HttpRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Content))
        return Results.BadRequest("Content is required");

    if (!TryResolveProvider(request, out var provider, out var providerError))
        return Results.BadRequest(providerError);

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

    var created = new List<KnowledgeChunk>();

    foreach (var paragraph in paragraphs)
    {
        var embedding = await provider!.GetEmbeddingAsync(paragraph);
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

app.MapPut("/knowledge/{id}", async (int id, KnowledgeRequest input, HttpRequest request, AppDbContext db) =>
{
    var chunk = await db.KnowledgeChunks.FindAsync(id);
    if (chunk is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(input.Content))
        return Results.BadRequest("Content is required");

    var trimmedContent = input.Content.Trim();
    var contentChanged = trimmedContent != chunk.Content;

    if (contentChanged)
    {
        if (!TryResolveProvider(request, out var provider, out var providerError))
            return Results.BadRequest(providerError);

        var embedding = await provider!.GetEmbeddingAsync(trimmedContent);
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

// Generate story endpoint using the selected AI provider, augmented with RAG over the knowledge base
app.MapPost("/stories/{id}/generate", async (int id, HttpRequest request, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();

    if (!TryResolveProvider(request, out var provider, out var providerError))
        return Results.BadRequest(providerError);

    var genre = story.GenreId.HasValue ? (await db.StoryGenres.FindAsync(story.GenreId))?.Name : "General";

    // Guardrails always apply when generating a story of a matching genre (or genre-less guardrails, which apply to all).
    var guardrails = await db.KnowledgeChunks
        .Where(k => k.AlwaysInclude && (k.GenreId == null || k.GenreId == story.GenreId))
        .Select(k => k.Content)
        .ToListAsync();

    // Retrieve relevant (non-guardrail) knowledge base chunks for this story's prompt/instructions.
    var retrievedContext = "";
    var queryText = $"{story.StoryPrompt} {story.StoryInstructions}".Trim();
    var queryEmbedding = string.IsNullOrEmpty(queryText) ? null : await provider!.GetEmbeddingAsync(queryText);

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

    try
    {
        var generatedText = await provider!.GenerateStoryAsync(
            story.StoryPrompt ?? string.Empty,
            story.StoryInstructions ?? string.Empty,
            guardrails,
            retrievedContext,
            genre ?? "General");

        story.GeneratedStory = generatedText;
        try
        {
            story.NarrativeTransportationScore =
                await provider.EvaluateNarrativeTransportationScoreAsync(generatedText);
        }
        catch
        {
            story.NarrativeTransportationScore = null;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new { generatedStory = generatedText, narrativeTransportationScore = story.NarrativeTransportationScore });
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error: {ex.Message}");
    }
});

app.Run();

static bool TryResolveProvider(HttpRequest request, out IAIProvider? provider, out string error)
{
    var requestedProvider = request.Headers["X-AI-Provider"].FirstOrDefault()?.Trim().ToLowerInvariant();
    requestedProvider = string.IsNullOrEmpty(requestedProvider) ? "gemini" : requestedProvider;

    if (requestedProvider is not ("gemini" or "cohere"))
    {
        provider = null;
        error = "Unsupported AI provider. Choose Gemini or Cohere.";
        return false;
    }

    var environmentVariable = requestedProvider == "gemini" ? "GEMINI_API_KEY" : "COHERE_API_KEY";
    var apiKey = Environment.GetEnvironmentVariable(environmentVariable);
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        provider = null;
        error = $"{environmentVariable} environment variable is not set for the selected AI provider.";
        return false;
    }

    provider = requestedProvider == "gemini"
        ? new GeminiProvider(apiKey)
        : new CohereProvider(apiKey);
    error = string.Empty;
    return true;
}

static IReadOnlyList<FeedbackInsight> BuildFeedbackInsights(IEnumerable<dynamic> evaluations)
{
    var definitions = new[]
    {
        (Category: "visualization", Label: "Visualization", ItemIndex: 0, Guidance: "Make the setting and events easier to picture with concrete locations, sensory details, and observable actions."),
        (Category: "involvement", Label: "Mental involvement", ItemIndex: 1, Guidance: "Strengthen the protagonist's goal, conflict, and stakes so the reader has a stronger reason to stay mentally involved."),
        (Category: "emotion", Label: "Emotional impact", ItemIndex: 2, Guidance: "Give important events clearer emotional consequences and show how the characters react to them."),
        (Category: "characters", Label: "Character imagery", ItemIndex: 4, Guidance: "Give characters distinctive traits, behavior, and dialogue that make them easier to visualize."),
        (Category: "suspense", Label: "Narrative curiosity", ItemIndex: 5, Guidance: "Create stronger unanswered questions and forward momentum so readers want to discover what happens next."),
        (Category: "attention-drift", Label: "Attention drift", ItemIndex: 6, Guidance: "Tighten pacing, remove repetition, and strengthen narrative tension where the story slows down."),
        (Category: "relevance", Label: "Everyday relevance", ItemIndex: 7, Guidance: "Connect the office setting to familiar workplace experiences, decisions, and consequences."),
        (Category: "perspective", Label: "Perspective change", ItemIndex: 8, Guidance: "Make the story's insight or change in perspective clearer through the conflict and resolution."),
    };
    var parsed = evaluations.Select(e => new
    {
        Responses = System.Text.Json.JsonSerializer.Deserialize<int[]>(e.ResponsesJson) ?? Array.Empty<int>(),
        Adjusted = System.Text.Json.JsonSerializer.Deserialize<int[]>(e.AdjustedResponsesJson) ?? Array.Empty<int>(),
    }).Where(e => e.Responses.Length == 15 && e.Adjusted.Length == 15).ToList();

    return definitions
        .Select(definition =>
        {
            var values = parsed.Select(e => definition.ItemIndex == 6 ? e.Responses[6] : e.Adjusted[definition.ItemIndex]).ToList();
            var average = values.Count == 0 ? 0 : values.Select(value => (double)value).Average();
            var repeatedPattern = values.Count >= 2 && (definition.ItemIndex == 6 ? average >= 5 : average < 4);
            return new FeedbackInsight(definition.Category, definition.Label, Math.Round(average, 2), values.Count, repeatedPattern ? definition.Guidance : null);
        })
        .Where(insight => insight.Guidance is not null)
        .ToList();
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

record NarrativeTransportationSurveyRequest(
    int[] Responses,
    string TransformedStory,
    string OfficeName,
    string OfficeDescription,
    string StoryVersion);

record FeedbackInsight(string Category, string Label, double Average, int EvaluationCount, string? Guidance);

record OfficeStoryTransformRequest(string OfficeName, string OfficeDescription);

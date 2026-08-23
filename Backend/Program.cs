using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- ADD THIS BLOCK FOR CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Default Vite React URL
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

// Ensure database is freshly created for development and seed genres
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var dbPath = "stories.db";

    // For development simplicity, if the DB exists remove it to ensure schema matches models
    if (System.IO.File.Exists(dbPath))
    {
        System.IO.File.Delete(dbPath);
    }

    db.Database.EnsureCreated();

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
        .Select(s => new { storyId = s.StoryId, storyInstructions = s.StoryInstructions, storyPrompt = s.StoryPrompt, generatedStory = s.GeneratedStory, genreId = s.GenreId, genreName = s.Genre != null ? s.Genre.Name : null })
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

// Generate story endpoint using Google Gemini
app.MapPost("/stories/{id}/generate", async (int id, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();

    var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (string.IsNullOrEmpty(geminiApiKey))
        return Results.BadRequest("GEMINI_API_KEY environment variable not set");

    var genre = story.GenreId.HasValue ? (await db.StoryGenres.FindAsync(story.GenreId))?.Name : "General";

    var prompt = $"""
Generate a single paragraph story (2-3 sentences) based on these inputs:
- Genre: {genre}
- Instructions: {story.StoryInstructions}
- Prompt: {story.StoryPrompt}

Write only the story paragraph, nothing else.
""";

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
                await db.SaveChangesAsync();

                return Results.Ok(new { generatedStory = generatedText });
            }
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error: {ex.Message}");
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

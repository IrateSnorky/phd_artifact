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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
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

// Stories endpoints
app.MapGet("/stories", async (AppDbContext db) =>
    await db.Stories.Select(s => new { storyId = s.StoryId, storyInstructions = s.StoryInstructions }).ToListAsync());

app.MapPost("/stories", async (Story story, AppDbContext db) =>
{
    db.Stories.Add(story);
    await db.SaveChangesAsync();
    return Results.Created($"/stories/{story.StoryId}", story);
});

app.MapPut("/stories/{id}", async (int id, Story input, AppDbContext db) =>
{
    var story = await db.Stories.FindAsync(id);
    if (story is null) return Results.NotFound();
    story.StoryInstructions = input.StoryInstructions;
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

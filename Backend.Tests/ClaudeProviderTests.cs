using System.Text.Json;
using Backend.Services;

namespace Backend.Tests;

public class ClaudeProviderTests
{
    private const string TestApiKey = "test-claude-api-key";
    private const string TestCohereApiKey = "test-cohere-api-key";

    [Fact]
    public async Task GenerateStoryAsync_WithValidPrompt_ReturnsNonEmptyString()
    {
        var provider = new ClaudeProvider(TestApiKey);
        var prompt = "Test prompt";
        var instructions = "Write a short story";
        var guardrails = new List<string> { "Keep it clean" };
        var retrievedContext = "Some context";
        var genre = "Science Fiction";

        var result = await provider.GenerateStoryAsync(prompt, instructions, guardrails, retrievedContext, genre);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task TransformStoryAsync_WithValidStory_ReturnsTransformedStory()
    {
        var provider = new ClaudeProvider(TestApiKey);
        var sourceStory = "A scientist discovered a new planet.";
        var officeName = "Tech Startup";
        var officeDescription = "A fast-moving software company";

        var result = await provider.TransformStoryAsync(sourceStory, officeName, officeDescription);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task ImproveStoryAsync_WithValidStory_ReturnsImprovedStory()
    {
        var provider = new ClaudeProvider(TestApiKey);
        var currentStory = "A scientist discovered a new planet at the tech startup.";
        var guardrails = new List<string> { "Make it more inspiring", "Add character depth" };
        var officeName = "Tech Startup";
        var officeDescription = "A fast-moving software company";

        var result = await provider.ImproveStoryAsync(currentStory, guardrails, officeName, officeDescription);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithValidText_ReturnsFloatArray()
    {
        var provider = new ClaudeProvider(TestApiKey, TestCohereApiKey);
        var text = "Sample text for embedding";

        var result = await provider.GetEmbeddingAsync(text);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, value => Assert.True(float.IsFinite(value)));
    }

    [Fact]
    public async Task GetEmbeddingAsync_WithoutCohereKey_ReturnsNull()
    {
        var provider = new ClaudeProvider(TestApiKey);
        var text = "Sample text for embedding";

        var result = await provider.GetEmbeddingAsync(text);

        Assert.Null(result);
    }

    [Fact]
    public async Task EvaluateNarrativeTransportationScoreAsync_WithValidStory_ReturnsScoreBetween9And63()
    {
        var provider = new ClaudeProvider(TestApiKey);
        var storyText = "A young adventurer sets out on a quest to find a legendary artifact.";

        var result = await provider.EvaluateNarrativeTransportationScoreAsync(storyText);

        Assert.NotNull(result);
        Assert.True(result >= 9 && result <= 63, $"Score {result} should be between 9 and 63");
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ClaudeProvider(null));
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ClaudeProvider(""));
    }

    [Fact]
    public void Name_Property_ReturnsClaudeName()
    {
        var provider = new ClaudeProvider(TestApiKey);

        Assert.Equal("Claude", provider.Name);
    }
}

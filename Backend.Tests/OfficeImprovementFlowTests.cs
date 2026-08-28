using System.Globalization;
using Backend.Services;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests;

public class OfficeImprovementFlowTests
{
    [Fact]
    public async Task ImproveFromSurvey_UsesGuardrailsAndResolver_WithoutOverwritingSourceStory()
    {
        var story = new Story
        {
            StoryId = 42,
            StoryPrompt = "A mysterious codebreaker in a law office.",
            StoryInstructions = "Keep the tone dramatic.",
            GeneratedStory = "A story in a law office."
        };

        var request = new NarrativeTransportationImprovementRequest(
            Responses: Enumerable.Repeat(1, 15).ToArray(),
            TransformedStory: story.GeneratedStory!,
            OfficeName: "Law firm",
            OfficeDescription: "Case review context.",
            StoryVersion: "v1");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-AI-Provider"] = "gemini";

        var result = await StoryImprovementService.ImproveStoryFromSurveyAsync(
            story,
            request,
            httpContext.Request,
            requestedProvider => new FakeProvider());

        Assert.NotNull(result);
        Assert.Equal("Improved law office story", result.TransformedStory);
        Assert.False(string.IsNullOrWhiteSpace(result.StoryVersion));
        Assert.NotEmpty(result.Guardrails);
        Assert.Contains(result.Guardrails, g => g.Contains("setting", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("A story in a law office.", story.GeneratedStory);
        Assert.Equal("v1", request.StoryVersion);
    }

    private sealed class FakeProvider : IAIProvider
    {
        public string Name => "gemini";

        public Task<string> GenerateStoryAsync(string prompt, string instructions, List<string> guardrails, string retrievedContext, string genre)
            => Task.FromResult("generated");

        public Task<string> TransformStoryAsync(string sourceStory, string officeName, string officeDescription)
            => Task.FromResult(sourceStory);

        public Task<string> ImproveStoryAsync(string currentStory, List<string> guardrails, string officeName, string officeDescription)
            => Task.FromResult("Improved law office story");

        public Task<float[]?> GetEmbeddingAsync(string text)
            => Task.FromResult<float[]?>(new[] { 1f, 2f, 3f });

        public Task<int?> EvaluateNarrativeTransportationScoreAsync(string storyText)
            => Task.FromResult<int?>(42);
    }
}

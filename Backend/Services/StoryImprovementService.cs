namespace Backend.Services;

public static class StoryImprovementService
{
    public static async Task<StoryImprovementResult> ImproveStoryFromSurveyAsync(
        Story story,
        NarrativeTransportationImprovementRequest request,
        HttpRequest httpRequest,
        Func<string?, IAIProvider?>? providerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(story);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(httpRequest);

        var guardrails = FeedbackInsightService.BuildImprovementGuardrails(request.Responses ?? []);

        if (!AIProviderResolver.Resolve(httpRequest, out var provider, out var providerError, providerFactory))
            throw new InvalidOperationException(providerError);

        var improvedStory = await provider!.ImproveStoryAsync(
            request.TransformedStory,
            guardrails.ToList(),
            request.OfficeName,
            request.OfficeDescription);

        return new StoryImprovementResult(
            improvedStory,
            Guid.NewGuid().ToString("N"),
            guardrails);
    }
}

public sealed record StoryImprovementResult(
    string TransformedStory,
    string StoryVersion,
    IReadOnlyList<string> Guardrails);

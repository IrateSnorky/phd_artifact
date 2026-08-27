using System.Text.Json;
using Backend.Services;

namespace Backend.Tests;

public class FeedbackInsightServiceTests
{
    [Fact]
    public void Build_ReturnsRepeatedLowScoringPattern()
    {
        var evaluations = CreateEvaluations(responseValue: 2);

        var insights = FeedbackInsightService.Build(evaluations);

        var visualization = Assert.Single(insights, insight => insight.Category == "visualization");
        Assert.Equal(2, visualization.Average);
        Assert.Equal(2, visualization.EvaluationCount);
        Assert.NotNull(visualization.Guidance);
    }

    [Fact]
    public void Build_ReturnsAttentionDriftForRepeatedHighScores()
    {
        var evaluations = CreateEvaluations(responseValue: 6);

        var insights = FeedbackInsightService.Build(evaluations);

        var attentionDrift = Assert.Single(insights, insight => insight.Category == "attention-drift");
        Assert.Equal(6, attentionDrift.Average);
    }

    [Fact]
    public void Build_DoesNotReturnPatternFromOneEvaluation()
    {
        var evaluations = CreateEvaluations(responseValue: 1).Take(1);

        Assert.Empty(FeedbackInsightService.Build(evaluations));
    }

    [Fact]
    public void BuildImprovementGuardrails_ReturnsGuidanceForLowScoresAndAttentionDrift()
    {
        var responses = Enumerable.Repeat(6, 15).ToArray();
        responses[0] = 2;
        responses[6] = 6;

        var guardrails = FeedbackInsightService.BuildImprovementGuardrails(responses);

        Assert.Contains(guardrails, guardrail => guardrail.Contains("setting and events easier to picture"));
        Assert.Contains(guardrails, guardrail => guardrail.Contains("Tighten pacing"));
    }

    [Fact]
    public void BuildImprovementGuardrails_RejectsInvalidResponseCount()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            FeedbackInsightService.BuildImprovementGuardrails(new[] { 1, 2 }));

        Assert.Contains("Exactly 15", exception.Message);
    }

    private static IEnumerable<(string ResponsesJson, string AdjustedResponsesJson)> CreateEvaluations(int responseValue)
    {
        var responses = Enumerable.Repeat(responseValue, 15).ToArray();
        var adjusted = responses.ToArray();
        adjusted[6] = 8 - responseValue;
        var responsesJson = JsonSerializer.Serialize(responses);
        var adjustedJson = JsonSerializer.Serialize(adjusted);
        return [(responsesJson, adjustedJson), (responsesJson, adjustedJson)];
    }
}

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

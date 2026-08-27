using System.Text.Json;

namespace Backend.Services;

public sealed record FeedbackInsight(string Category, string Label, double Average, int EvaluationCount, string? Guidance);

public static class FeedbackInsightService
{
    private static readonly (
        string Category,
        string Label,
        int ItemIndex,
        string Guidance
    )[] Definitions =
    [
        ("visualization", "Visualization", 0, "Make the setting and events easier to picture with concrete locations, sensory details, and observable actions."),
        ("involvement", "Mental involvement", 1, "Strengthen the protagonist's goal, conflict, and stakes so the reader has a stronger reason to stay mentally involved."),
        ("emotion", "Emotional impact", 2, "Give important events clearer emotional consequences and show how the characters react to them."),
        ("characters", "Character imagery", 4, "Give characters distinctive traits, behavior, and dialogue that make them easier to visualize."),
        ("suspense", "Narrative curiosity", 5, "Create stronger unanswered questions and forward momentum so readers want to discover what happens next."),
        ("attention-drift", "Attention drift", 6, "Tighten pacing, remove repetition, and strengthen narrative tension where the story slows down."),
        ("relevance", "Everyday relevance", 7, "Connect the office setting to familiar workplace experiences, decisions, and consequences."),
        ("perspective", "Perspective change", 8, "Make the story's insight or change in perspective clearer through the conflict and resolution."),
    ];

    public static IReadOnlyList<FeedbackInsight> Build(
        IEnumerable<(string ResponsesJson, string AdjustedResponsesJson)> evaluations)
    {
        var parsed = evaluations
            .Select(evaluation => new
            {
                Responses = JsonSerializer.Deserialize<int[]>(evaluation.ResponsesJson) ?? [],
                Adjusted = JsonSerializer.Deserialize<int[]>(evaluation.AdjustedResponsesJson) ?? [],
            })
            .Where(evaluation => evaluation.Responses.Length == 15 && evaluation.Adjusted.Length == 15)
            .ToList();

        return Definitions
            .Select(definition =>
            {
                var values = parsed
                    .Select(evaluation => definition.ItemIndex == 6
                        ? evaluation.Responses[6]
                        : evaluation.Adjusted[definition.ItemIndex])
                    .ToList();
                var average = values.Count == 0 ? 0 : values.Select(value => (double)value).Average();
                var repeatedPattern = values.Count >= 2 &&
                    (definition.ItemIndex == 6 ? average >= 5 : average < 4);
                return new FeedbackInsight(
                    definition.Category,
                    definition.Label,
                    Math.Round(average, 2),
                    values.Count,
                    repeatedPattern ? definition.Guidance : null);
            })
            .Where(insight => insight.Guidance is not null)
            .ToList();
    }

    public static IReadOnlyList<string> BuildImprovementGuardrails(IReadOnlyList<int> responses)
    {
        var score = NarrativeTransportationScoring.Calculate(responses);

        return Definitions
            .Where(definition => definition.ItemIndex == 6
                ? responses[6] >= 5
                : score.AdjustedResponses[definition.ItemIndex] < 4)
            .Select(definition => definition.Guidance)
            .ToList();
    }
}

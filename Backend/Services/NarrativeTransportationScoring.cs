namespace Backend.Services;

public sealed record NarrativeTransportationScore(int[] AdjustedResponses, int Total);

public static class NarrativeTransportationScoring
{
    public static NarrativeTransportationScore Calculate(IReadOnlyList<int> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);

        if (responses.Count != 15)
            throw new ArgumentException("Exactly 15 response values are required.", nameof(responses));

        if (responses.Any(response => response < 1 || response > 7))
            throw new ArgumentException("Each response must be between 1 and 7.", nameof(responses));

        var adjustedResponses = new int[responses.Count];
        var total = 0;
        for (var i = 0; i < responses.Count; i++)
        {
            adjustedResponses[i] = i == 6 ? 8 - responses[i] : responses[i];
            total += adjustedResponses[i];
        }

        return new NarrativeTransportationScore(adjustedResponses, total);
    }
}

using Backend.Services;

namespace Backend.Tests;

public class NarrativeTransportationScoringTests
{
    [Fact]
    public void Calculate_RejectsNullResponses()
    {
        IReadOnlyList<int>? responses = null;

        var exception = Assert.Throws<ArgumentNullException>(() => NarrativeTransportationScoring.Calculate(responses!));

        Assert.Equal("responses", exception.ParamName);
    }

    [Fact]
    public void Calculate_ReversesMindWanderingAndSumsAllResponses()
    {
        var result = NarrativeTransportationScoring.Calculate(Enumerable.Repeat(5, 15).ToArray());

        Assert.Equal(75 - 4, result.Total);
        Assert.Equal(1, result.AdjustedResponses[6]);
        Assert.All(result.AdjustedResponses.Where((_, index) => index != 6), value => Assert.Equal(5, value));
    }

    [Fact]
    public void Calculate_RejectsResponsesOutsideScale()
    {
        var responses = Enumerable.Repeat(1, 15).ToArray();
        responses[0] = 6;

        var exception = Assert.Throws<ArgumentException>(() => NarrativeTransportationScoring.Calculate(responses));

        Assert.Contains("between 1 and 5", exception.Message);
    }

    [Fact]
    public void Calculate_RejectsIncorrectItemCount()
    {
        var exception = Assert.Throws<ArgumentException>(() => NarrativeTransportationScoring.Calculate([1, 2]));

        Assert.Contains("Exactly 15", exception.Message);
    }
}

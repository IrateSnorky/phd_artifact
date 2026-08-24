public class NarrativeTransportationEvaluation
{
    public int NarrativeTransportationEvaluationId { get; set; }
    public int StoryId { get; set; }
    public Story Story { get; set; } = null!;
    public string ResponsesJson { get; set; } = "[]";
    public string AdjustedResponsesJson { get; set; } = "[]";
    public string TransformedStory { get; set; } = string.Empty;
    public string OfficeName { get; set; } = string.Empty;
    public string OfficeDescription { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public string StoryVersion { get; set; } = string.Empty;
    public int TotalScore { get; set; }
}
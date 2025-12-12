using Shared.Contracts.Enums;

namespace Shared.Contracts.Analysis;

public class AnalyzeSubmissionResponse
{
    public Guid SubmissionId { get; set; }
    public AnalysisStatus Status { get; set; }

    public bool? PlagiarismFlag { get; set; }
    public Guid? MatchedSubmissionId { get; set; }
    public decimal? SimilarityScore { get; set; }

    public string? WordCloudUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
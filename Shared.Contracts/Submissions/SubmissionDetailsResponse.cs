using Shared.Contracts.Enums;

namespace Shared.Contracts.Submissions;

public class SubmissionDetailsResponse
{
    public Guid SubmissionId { get; set; }
    public string StudentId { get; set; } = default!;
    public string AssignmentCode { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }

    public AnalysisStatus AnalysisStatus { get; set; }

    public bool? PlagiarismFlag { get; set; }
    public Guid? MatchedSubmissionId { get; set; }
    public decimal? SimilarityScore { get; set; }
    public string? ErrorMessage { get; set; }

    public string? WordCloudUrl { get; set; }
}
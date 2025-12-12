namespace Shared.Contracts.Reports;

public class SubmissionReportSummaryDto
{
    public Guid SubmissionId { get; set; }
    public string StudentId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }

    public bool PlagiarismFlag { get; set; }
    public Guid? MatchedSubmissionId { get; set; }

    public string? WordCloudUrl { get; set; }
}
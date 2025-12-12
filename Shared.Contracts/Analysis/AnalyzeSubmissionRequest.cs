namespace Shared.Contracts.Analysis;

public class AnalyzeSubmissionRequest
{
    public Guid SubmissionId { get; set; }
    public string StudentId { get; set; } = default!;
    public string AssignmentCode { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }

    public string FilePath { get; set; } = default!;
}
using Shared.Contracts.Enums;

namespace Shared.Contracts.Submissions;

public class SubmissionStoredResponse
{
    public Guid SubmissionId { get; set; }
    public string StudentId { get; set; } = default!;
    public string AssignmentCode { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }

    public string OriginalFileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
}
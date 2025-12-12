namespace Shared.Contracts.Reports;

public class AssignmentReportsResponse
{
    public string AssignmentCode { get; set; } = default!;
    public List<SubmissionReportSummaryDto> Reports { get; set; } = new();
}
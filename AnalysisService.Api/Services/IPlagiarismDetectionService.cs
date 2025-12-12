using AnalysisService.Api.Models;

namespace AnalysisService.Api.Services;

public interface IPlagiarismDetectionService
{
    Task<(bool isPlagiarism, Guid? matchedSubmissionId, decimal? similarityScore)>
        CheckPlagiarismAsync(Report newReport, CancellationToken cancellationToken);
}
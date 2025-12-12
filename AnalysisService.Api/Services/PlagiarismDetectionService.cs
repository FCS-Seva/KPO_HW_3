using AnalysisService.Api.Models;
using AnalysisService.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalysisService.Api.Services;

public class PlagiarismDetectionService : IPlagiarismDetectionService
{
    private readonly AnalysisDbContext _db;

    public PlagiarismDetectionService(AnalysisDbContext db)
    {
        _db = db;
    }

    public async Task<(bool isPlagiarism, Guid? matchedSubmissionId, decimal? similarityScore)>
        CheckPlagiarismAsync(Report newReport, CancellationToken cancellationToken)
    {
        var existing = await _db.Reports
            .AsNoTracking()
            .Where(r =>
                r.AssignmentCode == newReport.AssignmentCode &&
                r.StudentId != newReport.StudentId &&
                r.CreatedAt < newReport.CreatedAt &&
                r.NormalizedText == newReport.NormalizedText)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
            return (false, null, null);

        return (true, existing.SubmissionId, 1m);
    }
}
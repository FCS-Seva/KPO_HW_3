using AnalysisService.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Reports;

namespace AnalysisService.Api.Controllers;

[ApiController]
[Route("internal/reports")]
public class InternalReportsController : ControllerBase
{
    private readonly AnalysisDbContext _db;

    public InternalReportsController(AnalysisDbContext db)
    {
        _db = db;
    }

    [HttpGet("by-assignment/{assignmentCode}")]
    public async Task<ActionResult<AssignmentReportsResponse>> ByAssignment(
        string assignmentCode,
        CancellationToken cancellationToken)
    {
        var reports = await _db.Reports
            .AsNoTracking()
            .Where(r => r.AssignmentCode == assignmentCode)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var dto = new AssignmentReportsResponse
        {
            AssignmentCode = assignmentCode,
            Reports = reports.Select(r => new SubmissionReportSummaryDto
            {
                SubmissionId = r.SubmissionId,
                StudentId = r.StudentId,
                CreatedAt = r.CreatedAt,
                PlagiarismFlag = r.PlagiarismFlag ?? false,
                MatchedSubmissionId = r.MatchedSubmissionId,
                WordCloudUrl = r.WordCloudUrl
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpGet("{submissionId:guid}")]
    public async Task<ActionResult<Shared.Contracts.Submissions.SubmissionDetailsResponse>> Get(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var r = await _db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SubmissionId == submissionId, cancellationToken);

        if (r == null)
            return NotFound();

        var dto = new Shared.Contracts.Submissions.SubmissionDetailsResponse
        {
            SubmissionId = r.SubmissionId,
            StudentId = r.StudentId,
            AssignmentCode = r.AssignmentCode,
            CreatedAt = r.CreatedAt,
            AnalysisStatus = r.Status,
            PlagiarismFlag = r.PlagiarismFlag,
            MatchedSubmissionId = r.MatchedSubmissionId,
            SimilarityScore = r.SimilarityScore,
            ErrorMessage = r.ErrorMessage,
            WordCloudUrl = r.WordCloudUrl
        };

        return Ok(dto);
    }
}

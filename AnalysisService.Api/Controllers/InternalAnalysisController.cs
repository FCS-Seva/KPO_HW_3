using AnalysisService.Api.Models;
using AnalysisService.Api.Persistence;
using AnalysisService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Analysis;
using Shared.Contracts.Enums;

namespace AnalysisService.Api.Controllers;

[ApiController]
[Route("internal")]
public class InternalAnalysisController : ControllerBase
{
    private readonly AnalysisDbContext _db;
    private readonly ITextNormalizationService _normalizer;
    private readonly IPlagiarismDetectionService _plagiarism;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;

    public InternalAnalysisController(
        AnalysisDbContext db,
        ITextNormalizationService normalizer,
        IPlagiarismDetectionService plagiarism,
        IWebHostEnvironment env,
        IConfiguration configuration)
    {
        _db = db;
        _normalizer = normalizer;
        _plagiarism = plagiarism;
        _env = env;
        _configuration = configuration;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AnalyzeSubmissionResponse>> Analyze(
        [FromBody] AnalyzeSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        string? fileRoot = _configuration.GetSection("FileStorage")["RootPath"];
        fileRoot ??= "/files";

        var fullPath = Path.Combine(fileRoot, request.FilePath);

        var report = new Report
        {
            Id = Guid.NewGuid(),
            SubmissionId = request.SubmissionId,
            StudentId = request.StudentId,
            AssignmentCode = request.AssignmentCode,
            CreatedAt = request.CreatedAt
        };

        try
        {
            var text = await System.IO.File.ReadAllTextAsync(fullPath, cancellationToken);
            var normalized = _normalizer.Normalize(text);
            report.NormalizedText = normalized;

            var (isPlagiarism, matchedId, similarity) =
                await _plagiarism.CheckPlagiarismAsync(report, cancellationToken);

            report.Status = AnalysisStatus.Completed;
            report.PlagiarismFlag = isPlagiarism;
            report.MatchedSubmissionId = matchedId;
            report.SimilarityScore = similarity;

            var encodedText = Uri.EscapeDataString(normalized);
            report.WordCloudUrl = $"https://quickchart.io/wordcloud?text={encodedText}";
        }
        catch (Exception ex)
        {
            report.Status = AnalysisStatus.Error;
            report.ErrorMessage = ex.Message;
            report.NormalizedText = string.Empty;
        }

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new AnalyzeSubmissionResponse
        {
            SubmissionId = report.SubmissionId,
            Status = report.Status,
            PlagiarismFlag = report.PlagiarismFlag,
            MatchedSubmissionId = report.MatchedSubmissionId,
            SimilarityScore = report.SimilarityScore,
            WordCloudUrl = report.WordCloudUrl,
            ErrorMessage = report.ErrorMessage
        };

        return Ok(response);
    }
}

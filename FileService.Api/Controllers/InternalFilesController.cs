using FileService.Api.Models;
using FileService.Api.Persistence;
using FileService.Api.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Submissions;

namespace FileService.Api.Controllers;

[ApiController]
[Route("internal")]
public class InternalFilesController : ControllerBase
{
    private readonly FileServiceDbContext _db;
    private readonly IFileStorage _storage;

    public InternalFilesController(FileServiceDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpPost("files")]
    public async Task<ActionResult<SubmissionStoredResponse>> Upload(
        [FromForm] IFormFile file,
        [FromForm] string studentId,
        [FromForm] string assignmentCode,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            AssignmentCode = assignmentCode,
            CreatedAt = DateTimeOffset.UtcNow,
            OriginalFileName = file.FileName
        };

        var relativePath = await _storage.SaveFileAsync(submission.Id, file, cancellationToken);
        submission.FilePath = relativePath;

        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(cancellationToken);

        var result = new SubmissionStoredResponse
        {
            SubmissionId = submission.Id,
            StudentId = submission.StudentId,
            AssignmentCode = submission.AssignmentCode,
            CreatedAt = submission.CreatedAt,
            OriginalFileName = submission.OriginalFileName,
            FilePath = submission.FilePath
        };

        return Ok(result);
    }

    [HttpGet("submissions/{id:guid}")]
    public async Task<ActionResult<SubmissionStoredResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (submission == null)
        {
            return NotFound();
        }

        var result = new SubmissionStoredResponse
        {
            SubmissionId = submission.Id,
            StudentId = submission.StudentId,
            AssignmentCode = submission.AssignmentCode,
            CreatedAt = submission.CreatedAt,
            OriginalFileName = submission.OriginalFileName,
            FilePath = submission.FilePath
        };

        return Ok(result);
    }
}

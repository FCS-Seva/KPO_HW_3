using Microsoft.AspNetCore.Http;

namespace Gateway.Api.Dtos;

public class UploadSubmissionRequest
{
    public IFormFile File { get; set; } = default!;
    public string StudentId { get; set; } = default!;
    public string AssignmentCode { get; set; } = default!;
}
using System.Net.Http.Headers;
using Gateway.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Analysis;
using Shared.Contracts.Enums;
using Shared.Contracts.Reports;
using Shared.Contracts.Submissions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/submissions")]
public class SubmissionsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SubmissionsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<ActionResult<SubmissionCreatedResponse>> Upload(
        [FromForm] UploadSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var fileClient = _httpClientFactory.CreateClient("FileService");

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(request.File.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.File.ContentType);
        form.Add(fileContent, "file", request.File.FileName);
        form.Add(new StringContent(request.StudentId), "studentId");
        form.Add(new StringContent(request.AssignmentCode), "assignmentCode");

        SubmissionStoredResponse stored;

        try
        {
            var response = await fileClient.PostAsync("internal/files", form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "File service error");
            }

            stored = await response.Content.ReadFromJsonAsync<SubmissionStoredResponse>(cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException("Empty response from file service");
        }
        catch
        {
            return StatusCode(503, "File service unavailable");
        }

        var created = new SubmissionCreatedResponse
        {
            SubmissionId = stored.SubmissionId,
            StudentId = stored.StudentId,
            AssignmentCode = stored.AssignmentCode,
            CreatedAt = stored.CreatedAt
        };

        var analysisClient = _httpClientFactory.CreateClient("AnalysisService");

        try
        {
            var analyzeRequest = new AnalyzeSubmissionRequest
            {
                SubmissionId = stored.SubmissionId,
                StudentId = stored.StudentId,
                AssignmentCode = stored.AssignmentCode,
                CreatedAt = stored.CreatedAt,
                FilePath = stored.FilePath
            };

            var response = await analysisClient.PostAsJsonAsync("internal/analyze", analyzeRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                created.AnalysisStatus = AnalysisStatus.Error;
                created.ErrorMessage = $"Analysis service returned HTTP {(int)response.StatusCode}";
            }
            else
            {
                var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                jsonOptions.Converters.Add(new JsonStringEnumConverter());

                var analyzeResponse =
                    await response.Content.ReadFromJsonAsync<AnalyzeSubmissionResponse>(jsonOptions, cancellationToken);


                if (analyzeResponse != null)
                {
                    created.AnalysisStatus = analyzeResponse.Status;
                    created.PlagiarismFlag = analyzeResponse.PlagiarismFlag;
                    created.MatchedSubmissionId = analyzeResponse.MatchedSubmissionId;
                    created.SimilarityScore = analyzeResponse.SimilarityScore;
                    created.WordCloudUrl = analyzeResponse.WordCloudUrl;
                    created.ErrorMessage = analyzeResponse.ErrorMessage;
                }
            }
        }
        catch (Exception ex)
        {
            created.AnalysisStatus = AnalysisStatus.Error;
            created.ErrorMessage = $"Analysis service unavailable: {ex.Message}";
        }

        return CreatedAtAction(nameof(GetById), new { id = created.SubmissionId }, created);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubmissionDetailsResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var fileClient = _httpClientFactory.CreateClient("FileService");
        var analysisClient = _httpClientFactory.CreateClient("AnalysisService");

        SubmissionStoredResponse? stored = null;
        Shared.Contracts.Submissions.SubmissionDetailsResponse? report = null;

        try
        {
            var resp = await fileClient.GetAsync($"internal/submissions/{id}", cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                stored = await resp.Content.ReadFromJsonAsync<SubmissionStoredResponse>(cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // игнорируем, покажем частичные данные
        }

        try
        {
            var resp = await analysisClient.GetAsync($"internal/reports/{id}", cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                report = await resp.Content.ReadFromJsonAsync<Shared.Contracts.Submissions.SubmissionDetailsResponse>(
                    cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // игнорируем
        }

        if (stored == null && report == null)
            return NotFound();

        var result = new SubmissionDetailsResponse
        {
            SubmissionId = id,
            StudentId = stored?.StudentId ?? report?.StudentId ?? string.Empty,
            AssignmentCode = stored?.AssignmentCode ?? report?.AssignmentCode ?? string.Empty,
            CreatedAt = stored?.CreatedAt ?? report?.CreatedAt ?? DateTimeOffset.MinValue,
            AnalysisStatus = report?.AnalysisStatus ?? AnalysisStatus.Error,
            PlagiarismFlag = report?.PlagiarismFlag,
            MatchedSubmissionId = report?.MatchedSubmissionId,
            SimilarityScore = report?.SimilarityScore,
            ErrorMessage = report?.ErrorMessage,
            WordCloudUrl = report?.WordCloudUrl
        };

        return Ok(result);
    }
}

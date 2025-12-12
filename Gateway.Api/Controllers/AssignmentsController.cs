using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Reports;

namespace Gateway.Api.Controllers;

[ApiController]
[Route("api/assignments")]
public class AssignmentsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AssignmentsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("{assignmentCode}/reports")]
    public async Task<ActionResult<AssignmentReportsResponse>> GetReports(
        string assignmentCode,
        CancellationToken cancellationToken)
    {
        var analysisClient = _httpClientFactory.CreateClient("AnalysisService");

        try
        {
            var resp = await analysisClient.GetAsync(
                $"internal/reports/by-assignment/{assignmentCode}", cancellationToken);

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, "Analysis service error");

            var dto = await resp.Content.ReadFromJsonAsync<AssignmentReportsResponse>(cancellationToken: cancellationToken);
            if (dto == null)
                return StatusCode(500, "Empty response from analysis service");

            return Ok(dto);
        }
        catch
        {
            return StatusCode(503, "Analysis service unavailable");
        }
    }
}
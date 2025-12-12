using AnalysisService.Api.Persistence;
using AnalysisService.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AnalysisDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddScoped<ITextNormalizationService, TextNormalizationService>();
builder.Services.AddScoped<IPlagiarismDetectionService, PlagiarismDetectionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();

    var retries = 5;
    while (true)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch
        {
            retries--;
            if (retries <= 0)
                throw;

            Thread.Sleep(2000);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/submissions/{id:guid}/wordcloud",
    async (Guid id, AnalysisService.Api.Persistence.AnalysisDbContext db, CancellationToken cancellationToken) =>
    {
        var report = await db.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SubmissionId == id, cancellationToken);

        if (report == null || string.IsNullOrEmpty(report.WordCloudUrl))
            return Results.NotFound();

        using var http = new HttpClient();

        var resp = await http.GetAsync(report.WordCloudUrl, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)resp.StatusCode);
        }

        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/png";
        var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);

        return Results.File(bytes, contentType);
    });


app.MapControllers();

app.Run();
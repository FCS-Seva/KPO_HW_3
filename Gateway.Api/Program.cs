using System.Net.Http.Headers;
using Gateway.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Analysis;
using Shared.Contracts.Enums;
using Shared.Contracts.Reports;
using Shared.Contracts.Submissions;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("FileService", client =>
{
    var baseUrl = builder.Configuration["Services:FileService"] ?? "http://file-service:8080/";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddHttpClient("AnalysisService", client =>
{
    var baseUrl = builder.Configuration["Services:AnalysisService"] ?? "http://analysis-service:8080/";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
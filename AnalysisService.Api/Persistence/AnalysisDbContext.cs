using AnalysisService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnalysisService.Api.Persistence;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options)
        : base(options)
    {
    }

    public DbSet<Report> Reports => Set<Report>();
}
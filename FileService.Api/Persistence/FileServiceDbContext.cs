using FileService.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FileService.Api.Persistence;

public class FileServiceDbContext : DbContext
{
    public FileServiceDbContext(DbContextOptions<FileServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Submission> Submissions => Set<Submission>();
}
using Microsoft.AspNetCore.Http;

namespace FileService.Api.Storage;

public interface IFileStorage
{
    Task<string> SaveFileAsync(Guid submissionId, IFormFile file, CancellationToken cancellationToken);
}
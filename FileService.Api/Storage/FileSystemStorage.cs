using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FileService.Api.Storage;

public class FileSystemStorage : IFileStorage
{
    private readonly FileStorageOptions _options;

    public FileSystemStorage(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveFileAsync(Guid submissionId, IFormFile file, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.RootPath);

        var ext = Path.GetExtension(file.FileName);
        var fileName = submissionId.ToString() + ext;
        var path = Path.Combine(_options.RootPath, fileName);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(stream, cancellationToken);

        // относительный путь
        return fileName;
    }
}
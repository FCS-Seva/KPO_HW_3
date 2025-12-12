namespace Shared.Contracts.Common;

public class ErrorResponse
{
    public string Code { get; set; } = default!;
    public string Message { get; set; } = default!;
}
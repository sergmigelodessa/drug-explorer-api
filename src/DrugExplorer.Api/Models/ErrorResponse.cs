namespace DrugExplorer.Api.Models;

public class ErrorResponse
{
    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? TraceId { get; set; }
}

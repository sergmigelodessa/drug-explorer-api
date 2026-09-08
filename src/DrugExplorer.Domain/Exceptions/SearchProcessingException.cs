namespace DrugExplorer.Domain.Exceptions;

/// <summary>
/// Thrown when drug search processing fails (grouping, scoring, normalization)
/// </summary>
public class SearchProcessingException : Exception
{
    public SearchProcessingException(string message) : base(message)
    {
    }

    public SearchProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? ProcessingStep { get; set; }

    public string? Query { get; set; }
}

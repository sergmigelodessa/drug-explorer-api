namespace DrugExplorer.Domain.Exceptions;

/// <summary>
/// Thrown when an external API call fails or returns an error
/// </summary>
public class ExternalApiException : Exception
{
    public ExternalApiException(string message) : base(message)
    {
    }

    public ExternalApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? ApiName { get; set; }

    public int? HttpStatusCode { get; set; }

    public string? ResponseContent { get; set; }
}

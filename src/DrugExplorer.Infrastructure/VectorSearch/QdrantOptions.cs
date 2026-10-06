namespace DrugExplorer.Infrastructure.VectorSearch;

public class QdrantOptions
{
    public string BaseUrl { get; set; } = "http://localhost:6333/";

    public string CollectionName { get; set; } = "drug_chunks";

    // Must match the embedding model output (nomic-embed-text = 768).
    public int VectorSize { get; set; } = 768;

    public int TimeoutSeconds { get; set; } = 30;

    public string? ApiKey { get; set; }
}

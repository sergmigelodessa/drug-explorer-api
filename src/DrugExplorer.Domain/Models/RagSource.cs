namespace DrugExplorer.Domain.Models;

public class RagSource
{
    public string BrandName { get; init; } = string.Empty;

    public string GenericName { get; init; } = string.Empty;

    public string ChunkType { get; init; } = string.Empty;

    public string ChunkText { get; init; } = string.Empty;

    public double Similarity { get; init; }
}

using DrugExplorer.Domain.Enums;

namespace DrugExplorer.Domain.Models;

public class SemanticDrugResult
{
    public string BrandName { get; init; } = string.Empty;

    public string GenericName { get; init; } = string.Empty;

    public DrugChunkType ChunkType { get; init; }

    public string MatchedText { get; init; } = string.Empty;

    public double Score { get; init; }
}

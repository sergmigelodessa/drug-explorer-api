namespace DrugExplorer.Api.Dto;

public class SemanticDrugResultDto
{
    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public string ChunkType { get; set; } = string.Empty;

    public string MatchedText { get; set; } = string.Empty;

    public double Score { get; set; }
}

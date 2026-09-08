namespace DrugExplorer.Api.Dto;

public class RagSourceDto
{
    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public string ChunkType { get; set; } = string.Empty;

    public string ChunkText { get; set; } = string.Empty;

    public double Similarity { get; set; }
}

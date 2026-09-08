namespace DrugExplorer.Api.Dto;

public class SemanticSearchResponseDto
{
    public string Query { get; set; } = string.Empty;

    public List<SemanticDrugResultDto> Results { get; set; } = new();
}

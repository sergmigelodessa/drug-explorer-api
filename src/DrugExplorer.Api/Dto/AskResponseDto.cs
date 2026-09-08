namespace DrugExplorer.Api.Dto;

public class AskResponseDto
{
    public string Answer { get; set; } = string.Empty;

    public bool Grounded { get; set; }

    public List<RagSourceDto> Sources { get; set; } = new();
}

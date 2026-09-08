namespace DrugExplorer.Api.Dto;

public class DrugSearchResponseDto
{
    public string NormalizedQuery { get; set; } = string.Empty;

    public List<DrugGroupDto> Groups { get; set; } = new();

    public int TotalGroups => Groups.Count;

    public int TotalVariants => Groups.Sum(g => g.TotalVariants);
}

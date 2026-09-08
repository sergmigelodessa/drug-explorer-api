namespace DrugExplorer.Domain.Models;

public class DrugSearchResult
{
    public string NormalizedQuery { get; set; } = string.Empty;

    public List<DrugGroup> Groups { get; set; } = new();
}

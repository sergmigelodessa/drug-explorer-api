namespace DrugExplorer.Domain.Models;

public class RagAnswer
{
    public string Answer { get; init; } = string.Empty;

    public List<RagSource> Sources { get; init; } = new();

    // True when no source cleared the similarity threshold, so no LLM call was made.
    public bool Grounded { get; init; }
}

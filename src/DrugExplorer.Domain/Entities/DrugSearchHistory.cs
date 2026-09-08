namespace DrugExplorer.Domain.Entities;

public class DrugSearchHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string QueryText { get; set; } = string.Empty;

    public string NormalizedQuery { get; set; } = string.Empty;

    public int ResultCount { get; set; }

    public int ExecutionTimeMs { get; set; }

    public bool CacheHit { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

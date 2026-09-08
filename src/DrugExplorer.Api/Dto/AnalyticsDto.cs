namespace DrugExplorer.Api.Dto;

public class SearchAnalyticsDto
{
    public int TotalSearches { get; set; }
    public int CacheHits { get; set; }
    public double CacheHitRate { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public List<SearchQueryStatsDto> TopQueries { get; set; } = new();
}

public class SearchQueryStatsDto
{
    public string NormalizedQuery { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public int CacheHitCount { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public DateTime LastSearchedAt { get; set; }
}

public class SearchHistoryDto
{
    public Guid Id { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public string NormalizedQuery { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public int ExecutionTimeMs { get; set; }
    public bool CacheHit { get; set; }
    public DateTime CreatedAt { get; set; }
}

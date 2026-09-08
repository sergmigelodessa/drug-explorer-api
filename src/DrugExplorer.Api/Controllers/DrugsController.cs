using DrugExplorer.Api.Dto;
using DrugExplorer.Api.Mappings;
using DrugExplorer.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DrugExplorer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrugsController : ControllerBase
{
    private readonly IDrugSearchService _searchService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IRagAnswerService _ragAnswerService;
    private readonly DrugResultMapper _mapper;
    private readonly ILogger<DrugsController> _logger;
    private readonly IDrugSearchHistoryRepository _historyRepository;

    public DrugsController(
        IDrugSearchService searchService,
        ISemanticSearchService semanticSearchService,
        IRagAnswerService ragAnswerService,
        DrugResultMapper mapper,
        ILogger<DrugsController> logger,
        IDrugSearchHistoryRepository historyRepository)
    {
        _searchService = searchService;
        _semanticSearchService = semanticSearchService;
        _ragAnswerService = ragAnswerService;
        _mapper = mapper;
        _logger = logger;
        _historyRepository = historyRepository;
    }

    /// <summary>
    /// Search for drugs by query string
    /// </summary>
    /// <param name="query">Drug name or keyword to search for (minimum 2 characters)</param>
    /// <returns>Grouped search results with variants and metadata</returns>
    /// <response code="200">Successful search with results</response>
    /// <response code="400">Invalid input (empty or too short query)</response>
    /// <response code="502">External API error</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("search")]
    public async Task<ActionResult<DrugSearchResponseDto>> Search([FromQuery] string query)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query parameter is required and cannot be empty", nameof(query));

        if (query.Length < 2)
            throw new ArgumentException("Query must be at least 2 characters long", nameof(query));

        if (query.Length > 256)
            throw new ArgumentException("Query cannot exceed 256 characters", nameof(query));

        _logger.LogInformation("Searching for drugs with query: {Query}", query);

        var result = await _searchService.SearchAsync(query);
        var response = _mapper.MapSearchResult(result);

        _logger.LogInformation("Search completed successfully. Found {GroupCount} groups with {VariantCount} total variants",
            response.TotalGroups, response.TotalVariants);

        return Ok(response);
    }

    /// <summary>
    /// Semantic (vector similarity) search over ingested drug label chunks
    /// </summary>
    /// <param name="query">Free-text question or description (minimum 2 characters)</param>
    /// <param name="topK">Maximum number of distinct drugs to return (default 10)</param>
    /// <response code="200">Successful semantic search</response>
    /// <response code="400">Invalid input</response>
    [HttpGet("semantic-search")]
    public async Task<ActionResult<SemanticSearchResponseDto>> SemanticSearch(
        [FromQuery] string query,
        [FromQuery] int topK = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query parameter is required and cannot be empty", nameof(query));

        if (query.Length < 2)
            throw new ArgumentException("Query must be at least 2 characters long", nameof(query));

        if (topK < 1 || topK > 50)
            throw new ArgumentException("topK must be between 1 and 50", nameof(topK));

        _logger.LogInformation("Semantic search for query: {Query}", query);

        var results = await _semanticSearchService.SearchAsync(query, topK);
        var response = _mapper.MapSemanticResult(query, results);

        _logger.LogInformation("Semantic search completed. Found {Count} drugs", response.Results.Count);

        return Ok(response);
    }

    /// <summary>
    /// Ask a free-text question and get an answer grounded in ingested drug label text (RAG)
    /// </summary>
    /// <param name="request">The question to ask</param>
    /// <response code="200">Answer generated (may be ungrounded if no relevant context was found)</response>
    /// <response code="400">Invalid input</response>
    [HttpPost("ask")]
    public async Task<ActionResult<AskResponseDto>> Ask([FromBody] AskRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            throw new ArgumentException("Question is required and cannot be empty", nameof(request));

        if (request.Question.Length < 3)
            throw new ArgumentException("Question must be at least 3 characters long", nameof(request));

        if (request.Question.Length > 512)
            throw new ArgumentException("Question cannot exceed 512 characters", nameof(request));

        _logger.LogInformation("Ask request: {Question}", request.Question);

        var answer = await _ragAnswerService.AskAsync(request.Question);
        var response = _mapper.MapRagAnswer(answer);

        _logger.LogInformation("Ask completed. Grounded: {Grounded}, Sources: {SourceCount}",
            response.Grounded, response.Sources.Count);

        return Ok(response);
    }

    /// <summary>
    /// Get search analytics and statistics
    /// </summary>
    /// <returns>Analytics data including cache hit rate and top searches</returns>
    /// <response code="200">Analytics retrieved successfully</response>
    [HttpGet("analytics")]
    public async Task<ActionResult<SearchAnalyticsDto>> GetAnalytics()
    {
        _logger.LogInformation("Retrieving search analytics");

        var totalSearches = await _historyRepository.GetTotalSearchCountAsync();
        var cacheHits = await _historyRepository.GetCacheHitCountAsync();
        var avgExecutionTime = await _historyRepository.GetAverageExecutionTimeAsync();

        var cacheHitRate = totalSearches > 0 ? (double)cacheHits / totalSearches * 100 : 0;

        // Get top 10 queries by frequency
        var recentSearches = await _historyRepository.GetRecentSearchesAsync(limit: 1000);
        var topQueries = recentSearches
            .GroupBy(h => h.NormalizedQuery)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new SearchQueryStatsDto
            {
                NormalizedQuery = g.Key,
                SearchCount = g.Count(),
                CacheHitCount = g.Count(h => h.CacheHit),
                AverageExecutionTimeMs = g.Average(h => h.ExecutionTimeMs),
                LastSearchedAt = g.Max(h => h.CreatedAt)
            })
            .ToList();

        var analytics = new SearchAnalyticsDto
        {
            TotalSearches = totalSearches,
            CacheHits = cacheHits,
            CacheHitRate = cacheHitRate,
            AverageExecutionTimeMs = avgExecutionTime,
            TopQueries = topQueries
        };

        _logger.LogInformation("Analytics retrieved: {TotalSearches} searches, {CacheHitRate:F2}% cache hit rate",
            totalSearches, cacheHitRate);

        return Ok(analytics);
    }

    /// <summary>
    /// Get search history for a specific normalized query
    /// </summary>
    /// <param name="normalizedQuery">The normalized query to get history for</param>
    /// <param name="limit">Maximum number of records to return (default: 50)</param>
    /// <returns>List of historical searches</returns>
    /// <response code="200">History retrieved successfully</response>
    [HttpGet("history")]
    public async Task<ActionResult<List<SearchHistoryDto>>> GetHistory(
        [FromQuery] string normalizedQuery,
        [FromQuery] int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            throw new ArgumentException("normalizedQuery parameter is required", nameof(normalizedQuery));

        if (limit < 1 || limit > 1000)
            throw new ArgumentException("limit must be between 1 and 1000", nameof(limit));

        _logger.LogInformation("Retrieving search history for query: {NormalizedQuery}, limit: {Limit}",
            normalizedQuery, limit);

        var history = await _historyRepository.GetHistoryByQueryAsync(normalizedQuery, limit);
        var historyDtos = history
            .Select(h => new SearchHistoryDto
            {
                Id = h.Id,
                QueryText = h.QueryText,
                NormalizedQuery = h.NormalizedQuery,
                ResultCount = h.ResultCount,
                ExecutionTimeMs = h.ExecutionTimeMs,
                CacheHit = h.CacheHit,
                CreatedAt = h.CreatedAt
            })
            .ToList();

        return Ok(historyDtos);
    }
}

using DrugExplorer.Api.Dto;
using DrugExplorer.Api.Mappings;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace DrugExplorer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrugsController : ControllerBase
{
    private readonly IDrugSearchService _searchService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IRagAnswerService _ragAnswerService;
    private readonly IMedicamentSeedService _medicamentSeedService;
    private readonly IVectorIndexBuildService _vectorIndexBuildService;
    private readonly DrugResultMapper _mapper;
    private readonly ILogger<DrugsController> _logger;

    public DrugsController(
        IDrugSearchService searchService,
        ISemanticSearchService semanticSearchService,
        IRagAnswerService ragAnswerService,
        IMedicamentSeedService medicamentSeedService,
        IVectorIndexBuildService vectorIndexBuildService,
        DrugResultMapper mapper,
        ILogger<DrugsController> logger)
    {
        _searchService = searchService;
        _semanticSearchService = semanticSearchService;
        _ragAnswerService = ragAnswerService;
        _medicamentSeedService = medicamentSeedService;
        _vectorIndexBuildService = vectorIndexBuildService;
        _mapper = mapper;
        _logger = logger;
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

        var result = await _searchService.SearchAsync(query);
        var response = _mapper.MapSearchResult(result);

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

        var results = await _semanticSearchService.SearchAsync(query, topK);
        var response = _mapper.MapSemanticResult(query, results);

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

        var answer = await _ragAnswerService.AskAsync(request.Question);
        var response = _mapper.MapRagAnswer(answer);

        return Ok(response);
    }

    /// <summary>
    /// Bulk-populates the Drugs table by querying OpenFDA with a broad, built-in set of drug names
    /// </summary>
    /// <param name="targetCount">Desired number of new medicaments to insert (1 - 5000, default 1000)</param>
    /// <response code="200">Seeding completed (may stop early if OpenFDA runs out of matches)</response>
    /// <response code="400">Invalid input</response>
    [HttpPost("seed-medicaments")]
    public async Task<ActionResult<MedicamentSeedResponseDto>> SeedMedicaments(
        [FromQuery] int targetCount = 1000,
        CancellationToken cancellationToken = default)
    {
        if (targetCount < 1 || targetCount > 5000)
            throw new ArgumentException("targetCount must be between 1 and 5000", nameof(targetCount));

        _logger.LogInformation("Medicament seeding requested. Target count: {TargetCount}", targetCount);

        var result = await _medicamentSeedService.SeedAsync(targetCount, cancellationToken);

        var response = new MedicamentSeedResponseDto
        {
            TotalInserted = result.TotalInserted,
            TotalSkippedDuplicates = result.TotalSkippedDuplicates,
            TotalSkippedByLimit = result.TotalSkippedByLimit,
            QueriesUsed = result.QueriesUsed,
            RequestsMade = result.RequestsMade,
            ReachedTarget = result.ReachedTarget
        };

        _logger.LogInformation("Medicament seeding completed. Inserted: {Inserted}", response.TotalInserted);

        return Ok(response);
    }

    /// <summary>
    /// Embeds every Drugs chunk that is not yet in Qdrant (one by one; safe to re-run)
    /// </summary>
    /// <response code="200">Build finished</response>
    [HttpPost("build-vector-index")]
    public async Task<ActionResult<VectorIndexBuildResult>> BuildVectorIndex(CancellationToken cancellationToken = default)
    {
        var result = await _vectorIndexBuildService.BuildAsync(cancellationToken);
        return Ok(result);
    }
}

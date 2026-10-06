using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Services;

public class SemanticSearchService : ISemanticSearchService
{
    // Pull more raw chunk hits than requested results so grouping by drug still yields topK distinct drugs.
    private const int RAW_HITS_MULTIPLIER = 5;

    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<SemanticSearchService> _logger;

    public SemanticSearchService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<SemanticSearchService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<List<SemanticDrugResult>> SearchAsync(
        string query,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        if (await _vectorStore.CountAsync(cancellationToken) == 0)
        {
            _logger.LogWarning("Semantic search requested but vector store is empty");
            return new List<SemanticDrugResult>();
        }

        var queryVector = await _embeddingService.EmbedAsync(query, cancellationToken);
        var rawHits = await _vectorStore.SearchAsync(queryVector, topK * RAW_HITS_MULTIPLIER, cancellationToken);

        var results = rawHits
            .GroupBy(hit => hit.Embedding.DrugKey)
            .Select(group => group.OrderByDescending(hit => hit.Similarity).First())
            .OrderByDescending(hit => hit.Similarity)
            .Take(topK)
            .Select(hit => new SemanticDrugResult
            {
                BrandName = hit.Embedding.BrandName,
                GenericName = hit.Embedding.GenericName,
                ChunkType = hit.Embedding.ChunkType,
                MatchedText = hit.Embedding.ChunkText,
                Score = hit.Similarity
            })
            .ToList();

        _logger.LogInformation("Semantic search for {Query} returned {Count} drugs", query, results.Count);

        return results;
    }
}

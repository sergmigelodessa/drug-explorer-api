using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Infrastructure.VectorSearch;

// Singleton in-memory brute-force cosine-similarity store over DrugEmbeddings, refreshed from SQL Server on demand.
public class InMemoryVectorStore : IVectorStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InMemoryVectorStore> _logger;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    private volatile IReadOnlyList<(DrugEmbedding Entity, float[] Vector)> _cache =
        Array.Empty<(DrugEmbedding, float[])>();

    public InMemoryVectorStore(IServiceScopeFactory scopeFactory, ILogger<InMemoryVectorStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int Count => _cache.Count;

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDrugEmbeddingRepository>();
            var entities = await repository.GetAllAsync(cancellationToken);

            _cache = entities
                .Select(e => (e, JsonSerializer.Deserialize<float[]>(e.EmbeddingJson) ?? Array.Empty<float>()))
                .ToList();

            _logger.LogInformation("Vector store reloaded with {Count} embeddings", _cache.Count);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public IReadOnlyList<VectorSearchHit> Search(float[] queryVector, int topK)
    {
        var snapshot = _cache;

        return snapshot
            .Select(item => new VectorSearchHit(item.Entity, CosineSimilarity(queryVector, item.Vector)))
            .OrderByDescending(hit => hit.Similarity)
            .Take(topK)
            .ToList();
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0;
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}

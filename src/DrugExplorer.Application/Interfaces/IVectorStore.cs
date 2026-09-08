using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IVectorStore
{
    int Count { get; }

    // Reloads the in-memory cache from persistence; call after startup and after new embeddings are ingested.
    Task ReloadAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<VectorSearchHit> Search(float[] queryVector, int topK);
}

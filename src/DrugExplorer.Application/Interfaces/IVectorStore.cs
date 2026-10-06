using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IVectorStore
{
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default);
}

using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;

namespace DrugExplorer.Application.Interfaces;

public interface IDrugEmbeddingRepository
{
    Task<bool> ExistsAsync(string drugKey, DrugChunkType chunkType, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<DrugEmbedding> embeddings, CancellationToken cancellationToken = default);

    Task<List<DrugEmbedding>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

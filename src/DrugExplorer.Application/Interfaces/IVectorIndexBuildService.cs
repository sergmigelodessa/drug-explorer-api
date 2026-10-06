using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IVectorIndexBuildService
{
    // Walks the Drugs table and embeds every chunk that is not yet in the vector index, one by one.
    Task<VectorIndexBuildResult> BuildAsync(CancellationToken cancellationToken = default);
}

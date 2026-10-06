using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

// Write side of a vector index.
public interface IVectorIndexWriter
{
    // Creates the collection if missing; throws if an existing one has an incompatible configuration.
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);

    Task<HashSet<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task UpsertAsync(IReadOnlyCollection<VectorPoint> points, CancellationToken cancellationToken = default);
}

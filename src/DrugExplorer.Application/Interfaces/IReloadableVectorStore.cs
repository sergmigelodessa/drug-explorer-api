namespace DrugExplorer.Application.Interfaces;

// Only for stores that cache data in process memory (not needed for Qdrant).
public interface IReloadableVectorStore : IVectorStore
{
    Task ReloadAsync(CancellationToken cancellationToken = default);
}

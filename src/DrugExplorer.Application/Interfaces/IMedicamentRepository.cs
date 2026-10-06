using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Application.Interfaces;

public interface IMedicamentRepository
{
    Task<Drug> InsertAsync(Drug drug, CancellationToken cancellationToken = default);

    Task<Drug?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetExistingOpenFdaIdsAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<string, int>> GetGenericNameCountsAsync(CancellationToken cancellationToken = default);

    // Stable Id-ordered paging for full-table scans.
    Task<List<Drug>> GetPageAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<int> AddRangeAsync(IEnumerable<Drug> drugs, CancellationToken cancellationToken = default);
}
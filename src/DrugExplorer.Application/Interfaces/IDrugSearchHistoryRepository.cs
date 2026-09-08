using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Application.Interfaces;

public interface IDrugSearchHistoryRepository
{
    Task SaveSearchAsync(DrugSearchHistory history, CancellationToken cancellationToken = default);

    Task<List<DrugSearchHistory>> GetHistoryByQueryAsync(
        string normalizedQuery,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<List<DrugSearchHistory>> GetRecentSearchesAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalSearchCountAsync(CancellationToken cancellationToken = default);

    Task<int> GetCacheHitCountAsync(CancellationToken cancellationToken = default);

    Task<double> GetAverageExecutionTimeAsync(CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Persistence.Repositories;

public class DrugSearchHistoryRepository : IDrugSearchHistoryRepository
{
    private readonly DrugExplorerDbContext _context;

    public DrugSearchHistoryRepository(DrugExplorerDbContext context)
    {
        _context = context;
    }

    public async Task SaveSearchAsync(DrugSearchHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        _context.SearchHistory.Add(history);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DrugSearchHistory>> GetHistoryByQueryAsync(
        string normalizedQuery,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedQuery);

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        return await _context.SearchHistory
            .AsNoTracking()
            .Where(h => h.NormalizedQuery == normalizedQuery)
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DrugSearchHistory>> GetRecentSearchesAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        return await _context.SearchHistory
            .AsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalSearchCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SearchHistory
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetCacheHitCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SearchHistory
            .CountAsync(h => h.CacheHit, cancellationToken);
    }

    public async Task<double> GetAverageExecutionTimeAsync(CancellationToken cancellationToken = default)
    {
        var averageExecutionTime = await _context.SearchHistory
            .Select(h => (double?)h.ExecutionTimeMs)
            .AverageAsync(cancellationToken);

        return averageExecutionTime ?? 0;
    }
}

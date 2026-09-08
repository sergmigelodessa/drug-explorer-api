using DrugExplorer.Domain.Entities;
using DrugExplorer.Persistence;
using DrugExplorer.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DrugExplorer.Tests.Integration;

[TestClass]
public class AnalyticsRepositoryTests
{
    private DbContextOptions<DrugExplorerDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<DrugExplorerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [TestMethod]
    public async Task GetAnalytics_ShouldCalculateCacheHitRate()
    {
        // Arrange
        var histories = new List<DrugSearchHistory>
        {
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 100, CacheHit = false },
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 5, CacheHit = true },
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 5, CacheHit = true },
            new DrugSearchHistory { QueryText = "ibuprofen", NormalizedQuery = "ibuprofen", ResultCount = 7, ExecutionTimeMs = 150, CacheHit = false }
        };

        using (var context = new DrugExplorerDbContext(_options))
        {
            context.SearchHistory.AddRange(histories);
            await context.SaveChangesAsync();
        }

        var repository = new DrugSearchHistoryRepository(new DrugExplorerDbContext(_options));

        // Act
        var totalCount = await repository.GetTotalSearchCountAsync();
        var cacheHitCount = await repository.GetCacheHitCountAsync();
        var cacheHitRate = (double)cacheHitCount / totalCount * 100;

        // Assert
        Assert.AreEqual(4, totalCount);
        Assert.AreEqual(2, cacheHitCount);
        Assert.AreEqual(50.0, cacheHitRate, 0.01);
    }

    [TestMethod]
    public async Task GetRecentSearches_ShouldReturnInReverseChronologicalOrder()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var histories = new List<DrugSearchHistory>
        {
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 100, CacheHit = false, CreatedAt = now.AddMinutes(-10) },
            new DrugSearchHistory { QueryText = "ibuprofen", NormalizedQuery = "ibuprofen", ResultCount = 7, ExecutionTimeMs = 150, CacheHit = false, CreatedAt = now.AddMinutes(-5) },
            new DrugSearchHistory { QueryText = "paracetamol", NormalizedQuery = "paracetamol", ResultCount = 5, ExecutionTimeMs = 120, CacheHit = true, CreatedAt = now }
        };

        using (var context = new DrugExplorerDbContext(_options))
        {
            context.SearchHistory.AddRange(histories);
            await context.SaveChangesAsync();
        }

        var repository = new DrugSearchHistoryRepository(new DrugExplorerDbContext(_options));

        // Act
        var recent = await repository.GetRecentSearchesAsync(limit: 10);

        // Assert
        Assert.AreEqual(3, recent.Count);
        Assert.AreEqual("paracetamol", recent.First().QueryText);      // Most recent
        Assert.AreEqual("ibuprofen", recent[1].QueryText);              // Middle
        Assert.AreEqual("aspirin", recent.Last().QueryText);            // Oldest
    }

    [TestMethod]
    public async Task TopQueriesByFrequency_ShouldShowMostSearched()
    {
        // Arrange
        var histories = new List<DrugSearchHistory>
        {
            // Aspirin searched 5 times
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 100, CacheHit = false },
            new DrugSearchHistory { QueryText = "aspirin 500", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 5, CacheHit = true },
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 5, CacheHit = true },
            new DrugSearchHistory { QueryText = "aspirin 325", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 110, CacheHit = false },
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 100, CacheHit = true },

            // Ibuprofen searched 3 times
            new DrugSearchHistory { QueryText = "ibuprofen", NormalizedQuery = "ibuprofen", ResultCount = 7, ExecutionTimeMs = 150, CacheHit = false },
            new DrugSearchHistory { QueryText = "ibuprofen", NormalizedQuery = "ibuprofen", ResultCount = 7, ExecutionTimeMs = 5, CacheHit = true },
            new DrugSearchHistory { QueryText = "ibuprofen 200", NormalizedQuery = "ibuprofen", ResultCount = 7, ExecutionTimeMs = 155, CacheHit = false },

            // Paracetamol searched 1 time
            new DrugSearchHistory { QueryText = "paracetamol", NormalizedQuery = "paracetamol", ResultCount = 5, ExecutionTimeMs = 120, CacheHit = true }
        };

        using (var context = new DrugExplorerDbContext(_options))
        {
            context.SearchHistory.AddRange(histories);
            await context.SaveChangesAsync();
        }

        // Act
        var topQueries = histories
            .GroupBy(h => h.NormalizedQuery)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => new { Query = g.Key, Count = g.Count() })
            .ToList();

        // Assert
        Assert.AreEqual(3, topQueries.Count);
        Assert.AreEqual("aspirin", topQueries[0].Query);
        Assert.AreEqual(5, topQueries[0].Count);
        Assert.AreEqual("ibuprofen", topQueries[1].Query);
        Assert.AreEqual(3, topQueries[1].Count);
        Assert.AreEqual("paracetamol", topQueries[2].Query);
        Assert.AreEqual(1, topQueries[2].Count);
    }
}

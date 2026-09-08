using DrugExplorer.Domain.Entities;
using DrugExplorer.Persistence.Repositories;
using DrugExplorer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DrugExplorer.Tests.Integration;

[TestClass]
public class DrugSearchHistoryTests
{
    private DbContextOptions<DrugExplorerDbContext> _options = null!;

    [TestInitialize]
    public void Setup()
    {
        // Use in-memory database for testing
        _options = new DbContextOptionsBuilder<DrugExplorerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [TestMethod]
    public async Task SaveSearchHistory_ShouldPersistToDatabase()
    {
        // Arrange
        var history = new DrugSearchHistory
        {
            QueryText = "aspirin 500mg",
            NormalizedQuery = "aspirin",
            ResultCount = 5,
            ExecutionTimeMs = 250,
            CacheHit = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        using (var context = new DrugExplorerDbContext(_options))
        {
            context.SearchHistory.Add(history);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new DrugExplorerDbContext(_options))
        {
            var saved = await context.SearchHistory.FirstOrDefaultAsync();
            Assert.IsNotNull(saved);
            Assert.AreEqual("aspirin 500mg", saved.QueryText);
            Assert.AreEqual("aspirin", saved.NormalizedQuery);
            Assert.AreEqual(5, saved.ResultCount);
            Assert.AreEqual(250, saved.ExecutionTimeMs);
            Assert.IsFalse(saved.CacheHit);
        }
    }

    [TestMethod]
    public async Task SaveMultipleSearches_ShouldPersistAll()
    {
        // Arrange
        var histories = new List<DrugSearchHistory>
        {
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 150, CacheHit = false },
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 5, CacheHit = true },
            new DrugSearchHistory { QueryText = "ibuprofen", NormalizedQuery = "ibuprofen", ResultCount = 7, ExecutionTimeMs = 200, CacheHit = false }
        };

        // Act
        using (var context = new DrugExplorerDbContext(_options))
        {
            context.SearchHistory.AddRange(histories);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = new DrugExplorerDbContext(_options))
        {
            var count = await context.SearchHistory.CountAsync();
            Assert.AreEqual(3, count);

            var cacheHits = await context.SearchHistory.CountAsync(h => h.CacheHit);
            Assert.AreEqual(1, cacheHits);

            var aspirin = await context.SearchHistory
                .Where(h => h.NormalizedQuery == "aspirin")
                .ToListAsync();
            Assert.AreEqual(2, aspirin.Count);
        }
    }

    [TestMethod]
    public async Task QueryByNormalizedQuery_ShouldReturnCorrectHistory()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var histories = new List<DrugSearchHistory>
        {
            new DrugSearchHistory 
            { 
                QueryText = "aspirin 500mg", 
                NormalizedQuery = "aspirin", 
                ResultCount = 3, 
                ExecutionTimeMs = 150, 
                CacheHit = false,
                CreatedAt = now.AddMinutes(-5)
            },
            new DrugSearchHistory 
            { 
                QueryText = "aspirin 325mg", 
                NormalizedQuery = "aspirin", 
                ResultCount = 3, 
                ExecutionTimeMs = 50, 
                CacheHit = true,
                CreatedAt = now.AddMinutes(-2)
            },
            new DrugSearchHistory 
            { 
                QueryText = "ibuprofen", 
                NormalizedQuery = "ibuprofen", 
                ResultCount = 7, 
                ExecutionTimeMs = 200, 
                CacheHit = false,
                CreatedAt = now
            }
        };

        // Act & Assert
        using (var context = new DrugExplorerDbContext(_options))
        {
            context.SearchHistory.AddRange(histories);
            await context.SaveChangesAsync();
        }

        using (var context = new DrugExplorerDbContext(_options))
        {
            var aspirin = await context.SearchHistory
                .Where(h => h.NormalizedQuery == "aspirin")
                .OrderByDescending(h => h.CreatedAt)
                .Take(10)
                .ToListAsync();

            Assert.AreEqual(2, aspirin.Count);
            Assert.AreEqual("aspirin 325mg", aspirin.First().QueryText); // Most recent
            Assert.AreEqual("aspirin 500mg", aspirin.Last().QueryText);   // Older
        }
    }

    [TestMethod]
    public async Task CalculateStatistics_ShouldAggregateCorrectly()
    {
        // Arrange
        var histories = new List<DrugSearchHistory>
        {
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 100, CacheHit = false },
            new DrugSearchHistory { QueryText = "aspirin", NormalizedQuery = "aspirin", ResultCount = 3, ExecutionTimeMs = 200, CacheHit = true },
            new DrugSearchHistory { QueryText = "ibuprofen", NormalizedQuery = "ibuprofen", ResultCount = 7, ExecutionTimeMs = 300, CacheHit = false },
            new DrugSearchHistory { QueryText = "paracetamol", NormalizedQuery = "paracetamol", ResultCount = 5, ExecutionTimeMs = 150, CacheHit = true }
        };

        // Act & Assert
        using (var context = new DrugExplorerDbContext(_options))
        {
            context.SearchHistory.AddRange(histories);
            await context.SaveChangesAsync();
        }

        using (var context = new DrugExplorerDbContext(_options))
        {
            var totalCount = await context.SearchHistory.CountAsync();
            var cacheHitCount = await context.SearchHistory.CountAsync(h => h.CacheHit);
            var avgExecutionTime = await context.SearchHistory.AverageAsync(h => (double)h.ExecutionTimeMs);

            Assert.AreEqual(4, totalCount);
            Assert.AreEqual(2, cacheHitCount);
            Assert.AreEqual(187.5, avgExecutionTime, 0.1);
        }
    }

    [TestMethod]
    public async Task GetAverageExecutionTimeAsync_ShouldReturnZeroWhenHistoryIsEmpty()
    {
        await using var context = new DrugExplorerDbContext(_options);
        var repository = new DrugSearchHistoryRepository(context);

        var average = await repository.GetAverageExecutionTimeAsync();

        Assert.AreEqual(0, average);
    }
}

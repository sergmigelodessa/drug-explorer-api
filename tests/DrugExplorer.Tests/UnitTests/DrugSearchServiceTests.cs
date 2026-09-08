using DrugExplorer.Application.Interfaces;
using DrugExplorer.Application.Services;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DrugExplorer.Tests;

[TestClass]
public class DrugSearchServiceTests
{
    [TestMethod]
    public async Task SearchAsync_WhenCacheMiss_ReturnsGroupedResult_AndSavesHistory()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "Aspirin", GenericName = "Acetylsalicylic Acid", Manufacturer = "Pharma A" }
        };
        var expectedGroups = new List<DrugGroup>
        {
            new()
            {
                GroupName = "Aspirin",
                GenericName = "Acetylsalicylic Acid",
                TotalVariants = 1,
                Variants = new List<DrugVariant>
                {
                    new() { Name = "Aspirin 500mg", Manufacturer = "Pharma A" }
                }
            }
        };
        var normalizer = new Mock<IDrugNormalizationService>();
        normalizer.Setup(x => x.Normalize("Aspirin 500mg")).Returns("aspirin");

        var client = new Mock<IOpenFdaClient>();
        client.Setup(x => x.SearchAsync("aspirin")).ReturnsAsync(candidates);

        var groupingService = new Mock<IDrugGroupingService>();
        groupingService.Setup(x => x.Group(candidates)).Returns(expectedGroups);

        var historyRepository = new Mock<IDrugSearchHistoryRepository>();
        using var cache = CreateCache();

        var service = CreateService(normalizer.Object, client.Object, groupingService.Object, cache, historyRepository.Object);

        var result = await service.SearchAsync("Aspirin 500mg");

        Assert.AreEqual("aspirin", result.NormalizedQuery);
        Assert.AreSame(expectedGroups, result.Groups);
        client.Verify(x => x.SearchAsync("aspirin"), Times.Once);
        groupingService.Verify(x => x.Group(candidates), Times.Once);
        historyRepository.Verify(
            x => x.SaveSearchAsync(
                It.Is<DrugSearchHistory>(h =>
                    h.QueryText == "Aspirin 500mg" &&
                    h.NormalizedQuery == "aspirin" &&
                    h.ResultCount == 1 &&
                    h.CacheHit == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchAsync_WhenCalledTwiceWithSameQuery_UsesCachedResult_OnSecondCall()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "Ibuprofen", GenericName = "Ibuprofen", Manufacturer = "Pharma B" }
        };
        var groupedResult = new List<DrugGroup>
        {
            new()
            {
                GroupName = "Ibuprofen",
                GenericName = "Ibuprofen",
                TotalVariants = 1,
                Variants = new List<DrugVariant>
                {
                    new() { Name = "Ibuprofen 200mg", Manufacturer = "Pharma B" }
                }
            }
        };
        var normalizer = new Mock<IDrugNormalizationService>();
        normalizer.Setup(x => x.Normalize("Ibuprofen 200mg")).Returns("ibuprofen");

        var client = new Mock<IOpenFdaClient>();
        client.Setup(x => x.SearchAsync("ibuprofen")).ReturnsAsync(candidates);

        var groupingService = new Mock<IDrugGroupingService>();
        groupingService.Setup(x => x.Group(candidates)).Returns(groupedResult);

        var historyRepository = new Mock<IDrugSearchHistoryRepository>();
        using var cache = CreateCache();

        var service = CreateService(normalizer.Object, client.Object, groupingService.Object, cache, historyRepository.Object);

        var firstResult = await service.SearchAsync("Ibuprofen 200mg");
        var secondResult = await service.SearchAsync("Ibuprofen 200mg");

        Assert.AreSame(firstResult, secondResult);
        client.Verify(x => x.SearchAsync("ibuprofen"), Times.Once);
        groupingService.Verify(x => x.Group(candidates), Times.Once);
        historyRepository.Verify(
            x => x.SaveSearchAsync(
                It.Is<DrugSearchHistory>(h => h.CacheHit == false && h.ResultCount == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        historyRepository.Verify(
            x => x.SaveSearchAsync(
                It.Is<DrugSearchHistory>(h => h.CacheHit && h.ResultCount == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SearchAsync_WhenHistorySaveFails_ReturnsSearchResult()
    {
        var candidates = new List<DrugCandidate>();
        var expectedGroups = new List<DrugGroup>();
        var normalizer = new Mock<IDrugNormalizationService>();
        normalizer.Setup(x => x.Normalize("Paracetamol")).Returns("paracetamol");

        var client = new Mock<IOpenFdaClient>();
        client.Setup(x => x.SearchAsync("paracetamol")).ReturnsAsync(candidates);

        var groupingService = new Mock<IDrugGroupingService>();
        groupingService.Setup(x => x.Group(candidates)).Returns(expectedGroups);

        var historyRepository = new Mock<IDrugSearchHistoryRepository>();
        historyRepository
            .Setup(x => x.SaveSearchAsync(It.IsAny<DrugSearchHistory>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("history write failed"));
        using var cache = CreateCache();

        var service = CreateService(normalizer.Object, client.Object, groupingService.Object, cache, historyRepository.Object);

        var result = await service.SearchAsync("Paracetamol");

        Assert.AreEqual("paracetamol", result.NormalizedQuery);
        Assert.AreSame(expectedGroups, result.Groups);
        client.Verify(x => x.SearchAsync("paracetamol"), Times.Once);
        groupingService.Verify(x => x.Group(candidates), Times.Once);
        historyRepository.Verify(
            x => x.SaveSearchAsync(It.IsAny<DrugSearchHistory>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static MemoryCache CreateCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
    }

    private static DrugSearchService CreateService(
        IDrugNormalizationService normalizer,
        IOpenFdaClient client,
        IDrugGroupingService groupingService,
        IMemoryCache cache,
        IDrugSearchHistoryRepository historyRepository)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:EnableCaching"] = "true",
                ["CacheSettings:SearchCacheTtlMinutes"] = "10"
            })
            .Build();

        var scopeFactory = new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return new DrugSearchService(
            normalizer,
            client,
            groupingService,
            cache,
            configuration,
            NullLogger<DrugSearchService>.Instance,
            scopeFactory,
            historyRepository);
    }
}
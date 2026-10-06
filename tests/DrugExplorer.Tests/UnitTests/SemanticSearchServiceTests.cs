using DrugExplorer.Application.Interfaces;
using DrugExplorer.Application.Services;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;
using DrugExplorer.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DrugExplorer.Tests.UnitTests;

[TestClass]
public class SemanticSearchServiceTests
{
    [TestMethod]
    public async Task SearchAsync_WhenVectorStoreIsEmpty_ReturnsEmptyList_AndDoesNotEmbed()
    {
        var embeddingService = new Mock<IEmbeddingService>();
        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var service = CreateService(embeddingService.Object, vectorStore.Object);

        var result = await service.SearchAsync("aspirin");

        Assert.AreEqual(0, result.Count);
        embeddingService.Verify(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SearchAsync_GroupsHitsByDrugKey_AndKeepsHighestSimilarityPerDrug()
    {
        var embeddingService = new Mock<IEmbeddingService>();
        var queryVector = new float[] { 0.1f, 0.2f };
        embeddingService.Setup(x => x.EmbedAsync("aspirin", It.IsAny<CancellationToken>())).ReturnsAsync(queryVector);

        var lowHit = new VectorSearchHit(
            new DrugEmbedding { DrugKey = "aspirin|bayer", BrandName = "Bayer", GenericName = "Aspirin", ChunkType = DrugChunkType.Purpose, ChunkText = "Pain relief" },
            0.5);
        var highHit = new VectorSearchHit(
            new DrugEmbedding { DrugKey = "aspirin|bayer", BrandName = "Bayer", GenericName = "Aspirin", ChunkType = DrugChunkType.Warnings, ChunkText = "Consult a doctor" },
            0.9);
        var otherDrugHit = new VectorSearchHit(
            new DrugEmbedding { DrugKey = "ibuprofen|advil", BrandName = "Advil", GenericName = "Ibuprofen", ChunkType = DrugChunkType.Purpose, ChunkText = "Fever reducer" },
            0.7);

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
        vectorStore.Setup(x => x.SearchAsync(queryVector, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<VectorSearchHit> { lowHit, highHit, otherDrugHit });

        var service = CreateService(embeddingService.Object, vectorStore.Object);

        var result = await service.SearchAsync("aspirin", topK: 10);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Bayer", result[0].BrandName);
        Assert.AreEqual("Consult a doctor", result[0].MatchedText);
        Assert.AreEqual(0.9, result[0].Score);
        Assert.AreEqual("Advil", result[1].BrandName);
    }

    [TestMethod]
    public async Task SearchAsync_RequestsMoreRawHitsThanTopK_FromVectorStore()
    {
        var embeddingService = new Mock<IEmbeddingService>();
        var queryVector = new float[] { 0.1f };
        embeddingService.Setup(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryVector);

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        vectorStore.Setup(x => x.SearchAsync(queryVector, 15, It.IsAny<CancellationToken>())).ReturnsAsync(new List<VectorSearchHit>());

        var service = CreateService(embeddingService.Object, vectorStore.Object);

        await service.SearchAsync("aspirin", topK: 3);

        vectorStore.Verify(x => x.SearchAsync(queryVector, 15, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task SearchAsync_LimitsResultsToTopK()
    {
        var embeddingService = new Mock<IEmbeddingService>();
        var queryVector = new float[] { 0.1f };
        embeddingService.Setup(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryVector);

        var hits = Enumerable.Range(0, 5)
            .Select(i => new VectorSearchHit(
                new DrugEmbedding { DrugKey = $"drug{i}", BrandName = $"Drug{i}", ChunkType = DrugChunkType.Purpose, ChunkText = "text" },
                1.0 - i * 0.1))
            .ToList();

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);
        vectorStore.Setup(x => x.SearchAsync(queryVector, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(hits);

        var service = CreateService(embeddingService.Object, vectorStore.Object);

        var result = await service.SearchAsync("query", topK: 2);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Drug0", result[0].BrandName);
        Assert.AreEqual("Drug1", result[1].BrandName);
    }

    private static SemanticSearchService CreateService(IEmbeddingService embeddingService, IVectorStore vectorStore)
    {
        return new SemanticSearchService(embeddingService, vectorStore, NullLogger<SemanticSearchService>.Instance);
    }
}

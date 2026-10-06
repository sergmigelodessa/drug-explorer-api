using DrugExplorer.Application.Interfaces;
using DrugExplorer.Application.Services;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;
using DrugExplorer.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DrugExplorer.Tests.UnitTests;

[TestClass]
public class DrugKnowledgeIngestionServiceTests
{
    [TestMethod]
    public async Task IngestAsync_WhenCandidateHasNoNameFields_SkipsCandidate()
    {
        var candidates = new List<DrugCandidate> { new() { BrandName = "", GenericName = "" } };
        var embeddingService = new Mock<IEmbeddingService>();
        var repository = new Mock<IDrugEmbeddingRepository>();
        var vectorStore = new Mock<IVectorIndexWriter>();

        var service = CreateService(embeddingService.Object, repository.Object, vectorStore.Object);

        await service.IngestAsync(candidates);

        embeddingService.Verify(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<DrugEmbedding>>(), It.IsAny<CancellationToken>()), Times.Never);
        vectorStore.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<VectorPoint>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task IngestAsync_EmbedsAndSavesNewChunks_ThenUpsertsToVectorIndex()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "Aspirin", GenericName = "Acetylsalicylic Acid", Purpose = "Pain relief", Warnings = "Consult a doctor" }
        };

        var embeddingService = new Mock<IEmbeddingService>();
        embeddingService.Setup(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });

        var repository = new Mock<IDrugEmbeddingRepository>();
        repository.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<DrugChunkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        List<DrugEmbedding>? savedEmbeddings = null;
        repository
            .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<DrugEmbedding>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DrugEmbedding>, CancellationToken>((embeddings, _) => savedEmbeddings = embeddings.ToList())
            .Returns(Task.CompletedTask);

        var vectorStore = new Mock<IVectorIndexWriter>();

        var service = CreateService(embeddingService.Object, repository.Object, vectorStore.Object);

        await service.IngestAsync(candidates);

        Assert.IsNotNull(savedEmbeddings);
        Assert.AreEqual(2, savedEmbeddings!.Count);
        Assert.IsTrue(savedEmbeddings.Any(e => e.ChunkType == DrugChunkType.Purpose && e.ChunkText == "Pain relief"));
        Assert.IsTrue(savedEmbeddings.Any(e => e.ChunkType == DrugChunkType.Warnings && e.ChunkText == "Consult a doctor"));
        Assert.IsTrue(savedEmbeddings.All(e => e.DrugKey == "acetylsalicylic acid|aspirin"));
        vectorStore.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<VectorPoint>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task IngestAsync_SkipsChunksThatAlreadyExistInRepository()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "Aspirin", GenericName = "Acetylsalicylic Acid", Purpose = "Pain relief" }
        };

        var embeddingService = new Mock<IEmbeddingService>();
        var repository = new Mock<IDrugEmbeddingRepository>();
        repository.Setup(x => x.ExistsAsync(It.IsAny<string>(), DrugChunkType.Purpose, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var vectorStore = new Mock<IVectorIndexWriter>();

        var service = CreateService(embeddingService.Object, repository.Object, vectorStore.Object);

        await service.IngestAsync(candidates);

        embeddingService.Verify(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<DrugEmbedding>>(), It.IsAny<CancellationToken>()), Times.Never);
        vectorStore.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<VectorPoint>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task IngestAsync_DeduplicatesSameDrugAndChunkTypeWithinSameBatch()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "Aspirin", GenericName = "Acetylsalicylic Acid", Purpose = "Pain relief" },
            new() { BrandName = "Aspirin", GenericName = "Acetylsalicylic Acid", Purpose = "Pain relief" }
        };

        var embeddingService = new Mock<IEmbeddingService>();
        embeddingService.Setup(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        var repository = new Mock<IDrugEmbeddingRepository>();
        repository.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<DrugChunkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        List<DrugEmbedding>? savedEmbeddings = null;
        repository
            .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<DrugEmbedding>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DrugEmbedding>, CancellationToken>((embeddings, _) => savedEmbeddings = embeddings.ToList())
            .Returns(Task.CompletedTask);

        var vectorStore = new Mock<IVectorIndexWriter>();

        var service = CreateService(embeddingService.Object, repository.Object, vectorStore.Object);

        await service.IngestAsync(candidates);

        Assert.AreEqual(1, savedEmbeddings!.Count);
        embeddingService.Verify(x => x.EmbedAsync("Pain relief", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task IngestAsync_CapsProcessingAtMaxCandidatesPerIngest()
    {
        var candidates = Enumerable.Range(0, 10)
            .Select(i => new DrugCandidate { BrandName = $"Drug{i}", GenericName = $"Generic{i}", Purpose = "Some purpose" })
            .ToList();

        var embeddingService = new Mock<IEmbeddingService>();
        embeddingService.Setup(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        var repository = new Mock<IDrugEmbeddingRepository>();
        repository.Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<DrugChunkType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        List<DrugEmbedding>? savedEmbeddings = null;
        repository
            .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<DrugEmbedding>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<DrugEmbedding>, CancellationToken>((embeddings, _) => savedEmbeddings = embeddings.ToList())
            .Returns(Task.CompletedTask);

        var vectorStore = new Mock<IVectorIndexWriter>();

        var service = CreateService(embeddingService.Object, repository.Object, vectorStore.Object);

        await service.IngestAsync(candidates);

        Assert.AreEqual(5, savedEmbeddings!.Count);
    }

    [TestMethod]
    public async Task IngestAsync_WhenNoNewChunksToIngest_DoesNotCallAddRangeOrUpsert()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "Aspirin", GenericName = "Acetylsalicylic Acid" }
        };

        var embeddingService = new Mock<IEmbeddingService>();
        var repository = new Mock<IDrugEmbeddingRepository>();
        var vectorStore = new Mock<IVectorIndexWriter>();

        var service = CreateService(embeddingService.Object, repository.Object, vectorStore.Object);

        await service.IngestAsync(candidates);

        repository.Verify(x => x.AddRangeAsync(It.IsAny<IEnumerable<DrugEmbedding>>(), It.IsAny<CancellationToken>()), Times.Never);
        vectorStore.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<VectorPoint>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DrugKnowledgeIngestionService CreateService(
        IEmbeddingService embeddingService,
        IDrugEmbeddingRepository repository,
        IVectorIndexWriter vectorStore)
    {
        return new DrugKnowledgeIngestionService(
            embeddingService,
            repository,
            vectorStore,
            NullLogger<DrugKnowledgeIngestionService>.Instance);
    }
}

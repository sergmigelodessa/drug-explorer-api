using DrugExplorer.Application.Interfaces;
using DrugExplorer.Application.Services;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;
using DrugExplorer.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DrugExplorer.Tests.UnitTests;

[TestClass]
public class RagAnswerServiceTests
{
    [TestMethod]
    public async Task AskAsync_WhenVectorStoreIsEmpty_ReturnsUngroundedAnswer_AndDoesNotCallChat()
    {
        var embeddingService = new Mock<IEmbeddingService>();
        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.Count).Returns(0);
        var chatCompletionService = new Mock<IChatCompletionService>();

        var service = CreateService(embeddingService.Object, vectorStore.Object, chatCompletionService.Object);

        var result = await service.AskAsync("What is aspirin used for?");

        Assert.IsFalse(result.Grounded);
        Assert.AreEqual(0, result.Sources.Count);
        embeddingService.Verify(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        chatCompletionService.Verify(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task AskAsync_WhenNoHitsMeetSimilarityThreshold_ReturnsUngroundedAnswer()
    {
        var questionVector = new float[] { 0.1f };
        var embeddingService = new Mock<IEmbeddingService>();
        embeddingService.Setup(x => x.EmbedAsync("question", It.IsAny<CancellationToken>())).ReturnsAsync(questionVector);

        var lowSimilarityHit = new VectorSearchHit(
            new DrugEmbedding { BrandName = "Aspirin", ChunkType = DrugChunkType.Purpose, ChunkText = "Pain relief" },
            0.1);

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.Count).Returns(1);
        vectorStore.Setup(x => x.Search(questionVector, It.IsAny<int>())).Returns(new List<VectorSearchHit> { lowSimilarityHit });

        var chatCompletionService = new Mock<IChatCompletionService>();

        var service = CreateService(embeddingService.Object, vectorStore.Object, chatCompletionService.Object);

        var result = await service.AskAsync("question");

        Assert.IsFalse(result.Grounded);
        chatCompletionService.Verify(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task AskAsync_WhenHitsMeetThreshold_ReturnsGroundedAnswer_WithSources()
    {
        var questionVector = new float[] { 0.1f };
        var embeddingService = new Mock<IEmbeddingService>();
        embeddingService.Setup(x => x.EmbedAsync("What is aspirin used for?", It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionVector);

        var hit = new VectorSearchHit(
            new DrugEmbedding
            {
                BrandName = "Aspirin",
                GenericName = "Acetylsalicylic Acid",
                ChunkType = DrugChunkType.Purpose,
                ChunkText = "Used for pain relief"
            },
            0.8);

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.Count).Returns(1);
        vectorStore.Setup(x => x.Search(questionVector, It.IsAny<int>())).Returns(new List<VectorSearchHit> { hit });

        var chatCompletionService = new Mock<IChatCompletionService>();
        chatCompletionService
            .Setup(x => x.CompleteAsync(It.Is<string>(p => p.Contains("Used for pain relief")), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Aspirin is used for pain relief. [1] This is not medical advice.");

        var service = CreateService(embeddingService.Object, vectorStore.Object, chatCompletionService.Object);

        var result = await service.AskAsync("What is aspirin used for?");

        Assert.IsTrue(result.Grounded);
        Assert.AreEqual(1, result.Sources.Count);
        Assert.AreEqual("Aspirin", result.Sources[0].BrandName);
        Assert.AreEqual("Aspirin is used for pain relief. [1] This is not medical advice.", result.Answer);
    }

    [TestMethod]
    public async Task AskAsync_WhenChatCompletionReturnsEmpty_FallsBackToNoContextAnswer()
    {
        var questionVector = new float[] { 0.1f };
        var embeddingService = new Mock<IEmbeddingService>();
        embeddingService.Setup(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(questionVector);

        var hit = new VectorSearchHit(
            new DrugEmbedding { BrandName = "Aspirin", ChunkType = DrugChunkType.Purpose, ChunkText = "Pain relief" },
            0.9);

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.Count).Returns(1);
        vectorStore.Setup(x => x.Search(questionVector, It.IsAny<int>())).Returns(new List<VectorSearchHit> { hit });

        var chatCompletionService = new Mock<IChatCompletionService>();
        chatCompletionService.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);

        var service = CreateService(embeddingService.Object, vectorStore.Object, chatCompletionService.Object);

        var result = await service.AskAsync("question");

        Assert.IsTrue(result.Grounded);
        StringAssert.Contains(result.Answer, "couldn't find reliable information");
    }

    [TestMethod]
    public async Task AskAsync_FiltersOutHitsBelowSimilarityThreshold_ButKeepsQualifyingOnes()
    {
        var questionVector = new float[] { 0.1f };
        var embeddingService = new Mock<IEmbeddingService>();
        embeddingService.Setup(x => x.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(questionVector);

        var belowThreshold = new VectorSearchHit(
            new DrugEmbedding { BrandName = "Below", ChunkType = DrugChunkType.Purpose, ChunkText = "irrelevant" }, 0.2);
        var atThreshold = new VectorSearchHit(
            new DrugEmbedding { BrandName = "AtThreshold", ChunkType = DrugChunkType.Purpose, ChunkText = "relevant" }, 0.35);

        var vectorStore = new Mock<IVectorStore>();
        vectorStore.Setup(x => x.Count).Returns(2);
        vectorStore.Setup(x => x.Search(questionVector, It.IsAny<int>()))
            .Returns(new List<VectorSearchHit> { belowThreshold, atThreshold });

        var chatCompletionService = new Mock<IChatCompletionService>();
        chatCompletionService.Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("answer");

        var service = CreateService(embeddingService.Object, vectorStore.Object, chatCompletionService.Object);

        var result = await service.AskAsync("question");

        Assert.AreEqual(1, result.Sources.Count);
        Assert.AreEqual("AtThreshold", result.Sources[0].BrandName);
    }

    private static RagAnswerService CreateService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IChatCompletionService chatCompletionService)
    {
        return new RagAnswerService(embeddingService, vectorStore, chatCompletionService, NullLogger<RagAnswerService>.Instance);
    }
}

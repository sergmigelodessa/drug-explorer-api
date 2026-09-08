using System.Text;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Services;

public class RagAnswerService : IRagAnswerService
{
    private const int MAX_CONTEXT_CHUNKS = 5;
    private const double MIN_SIMILARITY_THRESHOLD = 0.35;

    private const string NO_CONTEXT_ANSWER =
        "I couldn't find reliable information about this in the drug knowledge base. Try rephrasing your question or searching for the drug by name first.";

    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<RagAnswerService> _logger;

    public RagAnswerService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IChatCompletionService chatCompletionService,
        ILogger<RagAnswerService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _chatCompletionService = chatCompletionService;
        _logger = logger;
    }

    public async Task<RagAnswer> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (_vectorStore.Count == 0)
        {
            _logger.LogWarning("Ask requested but vector store is empty");
            return new RagAnswer { Answer = NO_CONTEXT_ANSWER, Grounded = false };
        }

        var questionVector = await _embeddingService.EmbedAsync(question, cancellationToken);
        var hits = _vectorStore.Search(questionVector, MAX_CONTEXT_CHUNKS)
            .Where(hit => hit.Similarity >= MIN_SIMILARITY_THRESHOLD)
            .ToList();

        if (hits.Count == 0)
        {
            _logger.LogInformation("No context above similarity threshold for question: {Question}", question);
            return new RagAnswer { Answer = NO_CONTEXT_ANSWER, Grounded = false };
        }

        var sources = hits
            .Select(hit => new RagSource
            {
                BrandName = hit.Embedding.BrandName,
                GenericName = hit.Embedding.GenericName,
                ChunkType = hit.Embedding.ChunkType.ToString(),
                ChunkText = hit.Embedding.ChunkText,
                Similarity = hit.Similarity
            })
            .ToList();

        var prompt = BuildPrompt(question, sources);
        var answer = await _chatCompletionService.CompleteAsync(prompt, cancellationToken);

        _logger.LogInformation("Generated RAG answer using {SourceCount} sources", sources.Count);

        return new RagAnswer
        {
            Answer = string.IsNullOrWhiteSpace(answer) ? NO_CONTEXT_ANSWER : answer,
            Sources = sources,
            Grounded = true
        };
    }

    private static string BuildPrompt(string question, List<RagSource> sources)
    {
        var context = new StringBuilder();
        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            context.AppendLine($"[{i + 1}] {source.BrandName} ({source.ChunkType}): {source.ChunkText}");
        }

        return $"""
            You are a medical information assistant. Answer the question using ONLY the context below, which comes from official drug label text.
            If the context does not contain enough information, say so explicitly instead of guessing.
            Cite the numbered sources you used, e.g. "[1]". Always end your answer with: "This is not medical advice."

            Context:
            {context}

            Question: {question}

            Answer:
            """;
    }
}

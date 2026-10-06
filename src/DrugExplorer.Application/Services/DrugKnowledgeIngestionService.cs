using System.Text.Json;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Services;

public class DrugKnowledgeIngestionService : IDrugKnowledgeIngestionService
{
    private const int MAX_CANDIDATES_PER_INGEST = 5;

    private readonly IEmbeddingService _embeddingService;
    private readonly IDrugEmbeddingRepository _repository;
    private readonly IVectorIndexWriter _indexWriter;
    private readonly ILogger<DrugKnowledgeIngestionService> _logger;

    public DrugKnowledgeIngestionService(
        IEmbeddingService embeddingService,
        IDrugEmbeddingRepository repository,
        IVectorIndexWriter indexWriter,
        ILogger<DrugKnowledgeIngestionService> logger)
    {
        _embeddingService = embeddingService;
        _repository = repository;
        _indexWriter = indexWriter;
        _logger = logger;
    }

    public async Task IngestAsync(IReadOnlyList<DrugCandidate> candidates, CancellationToken cancellationToken = default)
    {
        var toIngest = new List<VectorPoint>();
        var seenInBatch = new HashSet<(string DrugKey, DrugChunkType ChunkType)>();

        // Cap per call so a single search doesn't trigger dozens of embedding calls.
        foreach (var candidate in candidates.Take(MAX_CANDIDATES_PER_INGEST))
        {
            var drugKey = BuildDrugKey(candidate);
            if (string.IsNullOrWhiteSpace(drugKey))
            {
                continue;
            }

            foreach (var (chunkType, text) in ExtractChunks(candidate))
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                // The same drug (different NDC package) can appear multiple times in one OpenFDA response.
                if (!seenInBatch.Add((drugKey, chunkType)))
                {
                    continue;
                }

                if (await _repository.ExistsAsync(drugKey, chunkType, cancellationToken))
                {
                    continue;
                }

                var vector = await _embeddingService.EmbedAsync(text, cancellationToken);

                var embedding = new DrugEmbedding
                {
                    Id = DrugChunks.BuildPointId(drugKey, chunkType),
                    DrugKey = drugKey,
                    BrandName = candidate.BrandName,
                    GenericName = candidate.GenericName,
                    ChunkType = chunkType,
                    ChunkText = text,
                    EmbeddingJson = JsonSerializer.Serialize(vector),
                    CreatedAt = DateTime.UtcNow
                };

                toIngest.Add(new VectorPoint(embedding, vector));
            }
        }

        if (toIngest.Count == 0)
        {
            return;
        }

        await _repository.AddRangeAsync(toIngest.Select(p => p.Embedding).ToList(), cancellationToken);
        await _indexWriter.EnsureCollectionAsync(cancellationToken);
        await _indexWriter.UpsertAsync(toIngest, cancellationToken);
        _logger.LogInformation("Ingested {Count} new drug knowledge chunks", toIngest.Count);
    }

    private static string BuildDrugKey(DrugCandidate candidate)
    {
        return DrugChunks.BuildDrugKey(candidate.GenericName, candidate.BrandName);
    }

    private static IEnumerable<(DrugChunkType ChunkType, string? Text)> ExtractChunks(DrugCandidate candidate)
    {
        yield return (DrugChunkType.Purpose, candidate.Purpose);
        yield return (DrugChunkType.Warnings, candidate.Warnings);
        yield return (DrugChunkType.DoNotUse, candidate.DoNotUse);
        yield return (DrugChunkType.AskDoctor, candidate.AskDoctor);
        yield return (DrugChunkType.AskDoctorOrPharmacist, candidate.AskDoctorOrPharmacist);
        yield return (DrugChunkType.PregnancyOrBreastFeeding, candidate.PregnancyOrBreastFeeding);
        yield return (DrugChunkType.DosageAndAdministration, candidate.DosageAndAdministration);
        yield return (DrugChunkType.ActiveIngredient, candidate.ActiveIngredient);
    }
}

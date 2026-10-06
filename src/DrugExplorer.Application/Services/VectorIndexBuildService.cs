using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Services;

public class VectorIndexBuildService : IVectorIndexBuildService
{
    private const int PAGE_SIZE = 100;

    private readonly IMedicamentRepository _medicamentRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorIndexWriter _indexWriter;
    private readonly ILogger<VectorIndexBuildService> _logger;

    public VectorIndexBuildService(
        IMedicamentRepository medicamentRepository,
        IEmbeddingService embeddingService,
        IVectorIndexWriter indexWriter,
        ILogger<VectorIndexBuildService> logger)
    {
        _medicamentRepository = medicamentRepository;
        _embeddingService = embeddingService;
        _indexWriter = indexWriter;
        _logger = logger;
    }

    public async Task<VectorIndexBuildResult> BuildAsync(CancellationToken cancellationToken = default)
    {
        await _indexWriter.EnsureCollectionAsync(cancellationToken);

        var result = new VectorIndexBuildResult();
        // The same drug can occur several times (different NDC packages); index each chunk once.
        var seen = new HashSet<Guid>();

        for (var skip = 0; ; skip += PAGE_SIZE)
        {
            var page = await _medicamentRepository.GetPageAsync(skip, PAGE_SIZE, cancellationToken);
            if (page.Count == 0)
            {
                break;
            }

            result.DrugsScanned += page.Count;

            var pending = new List<(Guid Id, string DrugKey, Drug Drug, Domain.Enums.DrugChunkType ChunkType, string Text)>();
            foreach (var drug in page)
            {
                var drugKey = DrugChunks.BuildDrugKey(drug.GenericName, drug.BrandName);
                if (drugKey.Length == 0)
                {
                    continue;
                }

                foreach (var (chunkType, text) in DrugChunks.Extract(drug))
                {
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    var id = DrugChunks.BuildPointId(drugKey, chunkType);
                    if (seen.Add(id))
                    {
                        pending.Add((id, drugKey, drug, chunkType, text));
                    }
                }
            }

            var existing = await _indexWriter.GetExistingIdsAsync(pending.Select(p => p.Id).ToList(), cancellationToken);

            foreach (var item in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (existing.Contains(item.Id))
                {
                    result.ChunksSkippedExisting++;
                    continue;
                }

                try
                {
                    var vector = await _embeddingService.EmbedAsync(item.Text, cancellationToken);

                    var embedding = new DrugEmbedding
                    {
                        Id = item.Id,
                        DrugKey = item.DrugKey,
                        BrandName = item.Drug.BrandName ?? string.Empty,
                        GenericName = item.Drug.GenericName ?? string.Empty,
                        ChunkType = item.ChunkType,
                        ChunkText = item.Text
                    };

                    await _indexWriter.UpsertAsync(new[] { new VectorPoint(embedding, vector) }, cancellationToken);
                    result.ChunksEmbedded++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    result.ChunksFailed++;
                    _logger.LogWarning(ex, "Failed to index chunk {ChunkType} of {DrugKey}", item.ChunkType, item.DrugKey);
                }
            }

            _logger.LogInformation(
                "Vector index build progress: {Scanned} drugs scanned, {Embedded} embedded, {Skipped} existing, {Failed} failed",
                result.DrugsScanned, result.ChunksEmbedded, result.ChunksSkippedExisting, result.ChunksFailed);
        }

        return result;
    }
}

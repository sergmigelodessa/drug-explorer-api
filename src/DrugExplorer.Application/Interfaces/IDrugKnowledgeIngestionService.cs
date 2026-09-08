using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IDrugKnowledgeIngestionService
{
    // Chunks candidate label text, embeds new chunks via Ollama and persists them. Safe to call repeatedly (dedup by DrugKey+ChunkType).
    Task IngestAsync(IReadOnlyList<DrugCandidate> candidates, CancellationToken cancellationToken = default);
}

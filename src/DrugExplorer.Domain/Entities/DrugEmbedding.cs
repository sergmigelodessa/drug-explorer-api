using DrugExplorer.Domain.Enums;

namespace DrugExplorer.Domain.Entities;

public class DrugEmbedding
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Normalized "generic|brand" identity used for dedup since OpenFDA set_id isn't mapped yet.
    public string DrugKey { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public DrugChunkType ChunkType { get; set; }

    public string ChunkText { get; set; } = string.Empty;

    // float[] vector serialized as JSON, e.g. "[0.01,-0.23,...]"
    public string EmbeddingJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

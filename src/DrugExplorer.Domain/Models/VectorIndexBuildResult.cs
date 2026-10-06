namespace DrugExplorer.Domain.Models;

public class VectorIndexBuildResult
{
    public int DrugsScanned { get; set; }

    public int ChunksEmbedded { get; set; }

    public int ChunksSkippedExisting { get; set; }

    public int ChunksFailed { get; set; }
}

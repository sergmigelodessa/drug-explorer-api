using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Domain.Models;

public record VectorPoint(DrugEmbedding Embedding, float[] Vector);

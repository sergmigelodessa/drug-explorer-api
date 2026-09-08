using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Domain.Models;

public record VectorSearchHit(DrugEmbedding Embedding, double Similarity);
